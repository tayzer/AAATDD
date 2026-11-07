using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class CoreTests
{
    [TestMethod]
    [DataRow("{\"z\":1,\"a\":2}", "{\"a\":2,\"z\":1}")]
    [DataRow("[1e30,4.50,2e-3,0.000000000000000000000000001]", "[1e+30,4.5,0.002,1e-27]")]
    [DataRow("[333333333.33333329,1E-6,1E-7,1E20,1E21,-0]", "[333333333.3333333,0.000001,1e-7,100000000000000000000,1e+21,0]")]
    [DataRow("{\"é\":\"<>&/\",\"a\":\"\\n\\t\"}", "{\"a\":\"\\n\\t\",\"é\":\"<>&/\"}")]
    public void CanonicalEncodingMatchesVectors(string input, string expected)
    {
        using var document = JsonDocument.Parse(input);
        Assert.AreEqual(expected, Encoding.UTF8.GetString(CanonicalJson.Encode(document.RootElement)));
    }

    [TestMethod]
    public void DuplicateJsonKeysAreRejected()
    {
        using var document = JsonDocument.Parse("{\"a\":1,\"a\":2}");
        Assert.ThrowsExactly<TadaException>(() => CanonicalJson.Encode(document.RootElement));
    }

    [TestMethod]
    public void WhitespaceInsideExpectationAndLineEndingsChangeIdentity()
    {
        Assert.AreNotEqual(CanonicalJson.Digest(Encoding.UTF8.GetBytes("Then message is \"out of stock\"\n")),
            CanonicalJson.Digest(Encoding.UTF8.GetBytes("Then message is \"outofstock\"\n")));
        Assert.AreNotEqual(CanonicalJson.Digest([10]), CanonicalJson.Digest([13, 10]));
    }

    [TestMethod]
    public void ApprovalRejectsEditedReviewedInputs()
    {
        using var project = new Project();
        var store = project.Store;
        var review = store.Review(store.Prepare());
        File.AppendAllText(project.Feature, "\n# edit after review\n");
        Assert.ThrowsExactly<TadaException>(() => store.Approve(review.Id, "human"));
    }

    [TestMethod]
    public void ApprovalPinsImmutableSnapshotAndAddedMaterialInputsInvalidateIt()
    {
        using var project = new Project();
        var store = project.Store;
        string baseline = store.Prepare();
        var approval = store.Approve(store.Review(baseline).Id, "human");
        Assert.AreEqual(baseline, approval.Baseline);
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "NewAssertion.cs"), "// new material input");
        Assert.ThrowsExactly<TadaException>(() => store.CheckCurrent(baseline));
        Assert.AreEqual(baseline, CanonicalJson.Identity(store.Read(baseline)));
    }

    [TestMethod]
    public void SnapshotTamperingIsRejected()
    {
        using var project = new Project();
        string baseline = project.Store.Prepare();
        File.AppendAllText(Path.Combine(project.Store.Snapshot(baseline), "features", "sample.feature"), "\n# tamper");
        Assert.ThrowsExactly<TadaException>(() => project.Store.Read(baseline));
    }

    [TestMethod]
    public async Task ConcurrentPreparationPublishesOneConsistentSnapshot()
    {
        using var project = new Project();
        string[] identities = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => project.Store.Prepare())));
        Assert.HasCount(1, identities.Distinct().ToArray());
        Assert.AreEqual(identities[0], CanonicalJson.Identity(project.Store.Read(identities[0])));
        Assert.HasCount(1, Directory.GetFiles(Path.Combine(project.Root, ".tada", "baselines"), "*.json"));
        Assert.HasCount(1, Directory.GetDirectories(Path.Combine(project.Root, ".tada", "snapshots")));
        Assert.IsEmpty(Directory.GetDirectories(Path.Combine(project.Root, ".tada", "scratch")));
    }

    [TestMethod]
    public async Task ChangeAfterApprovalPreventsAnyExecutionCommand()
    {
        using var project = new Project();
        string baseline = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(baseline).Id, "automated approval-drift control; not human acceptance");
        File.AppendAllText(project.Feature, "\n# changed after approval\n");
        var result = await new RunEngine(project.Store).Run(baseline);
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(reason => reason.Detail.Contains("Oracle inputs changed", StringComparison.Ordinal)));
        var record = Data.Read<RunRecord>(Path.Combine(project.Root, ".tada", "runs", result.RunId, "run.json"));
        Assert.IsEmpty(record.Commands);
    }

    [TestMethod]
    public async Task OracleEditRacingApprovalCannotAuthoriseExecutionOfChangedBytes()
    {
        for (int iteration = 0; iteration < 8; iteration++)
        {
            using var project = new Project();
            string baseline = project.Store.Prepare();
            string snapshotDigest = project.Store.Read(baseline).Files.Single(file => file.Path == "features/sample.feature").Digest;
            var review = project.Store.Review(baseline);
            using var barrier = new Barrier(2);
            Approval? accepted = null;
            Exception? refused = null;
            Task approve = Task.Run(() =>
            {
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Approval race did not synchronise.");
                try { accepted = project.Store.Approve(review.Id, "automated approval-race control; not human acceptance"); }
                catch (Exception error) when (error is TadaException or IOException) { refused = error; }
            });
            Task edit = Task.Run(() =>
            {
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Oracle race did not synchronise.");
                File.AppendAllText(project.Feature, "\n# concurrent post-review oracle edit\n");
            });
            await Task.WhenAll(approve, edit);
            Assert.IsTrue(accepted != null || refused != null);
            Assert.AreEqual(snapshotDigest, project.Store.Read(baseline).Files.Single(file => file.Path == "features/sample.feature").Digest);
            var result = await new RunEngine(project.Store).Run(baseline);
            Assert.AreEqual("INDETERMINATE", result.Outcome);
            var record = Data.Read<RunRecord>(Path.Combine(project.Root, ".tada", "runs", result.RunId, "run.json"));
            Assert.IsEmpty(record.Commands, "A raced approval authorised execution after oracle drift.");
        }
    }

    [TestMethod]
    public void ScenarioOutlinesCannotDisappearFromInventory()
    {
        using var project = new Project();
        File.WriteAllText(project.Feature, "@REQ_ONE\nFeature: Example\n @AC_ONE\n Scenario Outline: Values\n  Given value <n>\n Examples:\n  |n|\n  |1|\n");
        Assert.ThrowsExactly<TadaException>(() => project.Store.Prepare());
    }

    [TestMethod]
    public void DuplicateScenarioIdsAreRejectedAcrossFeatures()
    {
        using var project = new Project();
        File.Copy(project.Feature, Path.Combine(project.Root, "features", "another.feature"));
        Assert.ThrowsExactly<TadaException>(() => project.Store.Prepare());
    }

    [TestMethod]
    public void ReplacementCannotDropPreviouslyApprovedObligations()
    {
        using var project = new Project();
        string first = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(first).Id, "human");
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("AC_ONE", "AC_OTHER"));
        string second = project.Store.Prepare();
        Assert.ThrowsExactly<TadaException>(() => project.Store.Approve(project.Store.Review(second, first).Id, "human", first));
    }

    [TestMethod]
    public void ReplacementRequiresReviewOfTheActualSupersededRevision()
    {
        using var project = new Project();
        string first = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(first).Id, "synthetic approval control");
        File.AppendAllText(project.Feature, "\n# reviewed amendment\n");
        string next = project.Store.Prepare();
        Assert.ThrowsExactly<TadaException>(() => project.Store.Approve(project.Store.Review(next).Id, "synthetic approval control", first));
        project.Store.Approve(project.Store.Review(next, first).Id, "synthetic approval control", first);
        Assert.AreEqual(next, project.Store.Approved(next).Baseline);
        Assert.ThrowsExactly<TadaException>(() => project.Store.Approved(first));
    }

    [TestMethod]
    [DataRow("../outside")]
    [DataRow("a/../file")]
    [DataRow("a//file")]
    [DataRow("C:/file")]
    [DataRow("file:stream")]
    [DataRow("CON.txt")]
    [DataRow("name.")]
    [DataRow("features\\sample.feature")]
    public void UnsafePathsAreRejected(string path)
    {
        using var project = new Project();
        Assert.ThrowsExactly<TadaException>(() => new Repository(project.Root).Resolve(path));
    }

    [TestMethod]
    public void InitPreservesExistingAgentInstructionsAndIsRepeatable()
    {
        using var project = new Project();
        File.WriteAllText(Path.Combine(project.Root, "AGENTS.md"), "existing authority");
        var first = Onboarding.Initialise(project.Root);
        Assert.IsGreaterThan(0, first.Created.Length);
        CollectionAssert.AreEqual(new[] { "AGENTS.md" }, first.Updated);
        string enabled = File.ReadAllText(Path.Combine(project.Root, "AGENTS.md"));
        StringAssert.StartsWith(enabled, "existing authority");
        StringAssert.Contains(enabled, Onboarding.AgentInstructions);
        var second = Onboarding.Initialise(project.Root);
        Assert.AreEqual(0, second.Created.Length);
        Assert.AreEqual(0, second.Updated.Length);
        Assert.AreEqual(enabled, File.ReadAllText(Path.Combine(project.Root, "AGENTS.md")));
    }

    [TestMethod]
    public void EmptyBusinessSuiteIsNotSuccess()
    {
        using var project = new Project();
        File.Delete(project.Feature);
        Assert.ThrowsExactly<TadaException>(() => project.Store.Prepare());
    }

    [TestMethod]
    public void MissingRunMetadataReturnsIndeterminateInsteadOfCrashing()
    {
        using var project = new Project();
        Assert.AreEqual("INDETERMINATE", new RunEngine(project.Store).Verify(Guid.NewGuid().ToString("N")).Outcome);
    }

    [TestMethod]
    public void MissingRequiredMetadataFieldsAreNotDefaultedToSuccess()
    {
        using var project = new Project();
        string path = Path.Combine(project.Root, "invalid.json");
        File.WriteAllText(path, "{\"id\":\"misleading-success\"}");
        Assert.ThrowsExactly<JsonException>(() => Data.Read<RunRecord>(path));
    }

    [TestMethod]
    public void DuplicateMetadataKeysAreRejectedBeforeDeserialisation()
    {
        using var project = new Project();
        string path = Path.Combine(project.Root, "invalid.json");
        File.WriteAllText(path, "{\"id\":\"one\",\"id\":\"two\"}");
        Assert.ThrowsExactly<TadaException>(() => Data.Read<Review>(path));
    }

    [TestMethod]
    public async Task ProcessDeadlineTerminatesOwnedProcessAndRetainsDiagnostics()
    {
        using var project = new Project();
        var result = await Processes.Execute("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", "Write-Output 'started'; Start-Sleep -Seconds 20"], project.Root, 1);
        Assert.IsTrue(result.TimedOut);
        Assert.AreNotEqual(0, result.ExitCode);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), result.Finished - result.Started);
    }

    internal sealed class Project : IDisposable
    {
        public const string FeatureText = "@REQ_ONE\nFeature: Example\n @AC_ONE\n Scenario: Check\n  Given value exists\n  When value changes\n  Then result is correct\n";
        private readonly string parent;
        private readonly string ownedId = Guid.NewGuid().ToString("N");
        public string Root { get; }
        public string Feature => Path.Combine(Root, "features", "sample.feature");
        public BaselineStore Store => new(new Repository(Root));
        public Project()
        {
            parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.GetEnvironmentVariable("TADA_TEST_ROOT") ?? Path.Combine(Path.GetTempPath(), "TadaTests")));
            Root = Path.Combine(parent, ownedId);
            Directory.CreateDirectory(Path.Combine(Root, "features"));
            Directory.CreateDirectory(Path.Combine(Root, "acceptance"));
            File.WriteAllText(Feature, FeatureText);
            File.WriteAllText(Path.Combine(Root, "global.json"), "{\"sdk\":{\"version\":\"10.0.403\"}}");
            File.WriteAllText(Path.Combine(Root, "NuGet.Config"), "<configuration/>");
            File.WriteAllText(Path.Combine(Root, "acceptance", "Acceptance.csproj"), "<Project/>");
            File.WriteAllText(Path.Combine(Root, "acceptance", "packages.lock.json"), "{}");
            Data.WriteNew(Path.Combine(Root, "tada.json"), new Config(1, ["features", "acceptance"], new Execution("acceptance/Acceptance.csproj", "Acceptance.dll", "messages.ndjson")));
        }
        public void Dispose()
        {
            if (Path.GetDirectoryName(Root) != parent || Path.GetFileName(Root) != ownedId)
                throw new InvalidOperationException("Unexpected test cleanup target.");
            foreach (string file in Directory.EnumerateFiles(Root, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, recursive: true);
        }
    }
}
