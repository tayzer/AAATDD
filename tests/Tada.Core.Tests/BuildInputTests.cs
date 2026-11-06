using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class BuildInputTests
{
    [TestMethod]
    public void LinkedSourceOutsideSelectedOracleBlocksBuild()
    {
        using var fixture = new Fixture();
        string source = fixture.File("shared/Assertion.cs", "// unreviewed assertion");
        fixture.Evaluate(source);
        var error = Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
        StringAssert.Contains(error.Message, "Unreviewed harness build input");
        StringAssert.Contains(error.Message, source);
    }

    [TestMethod]
    public void ExplicitlyCapturedLinkedSourceHasReviewedByteIdentity()
    {
        using var fixture = new Fixture();
        string source = fixture.File("shared/Assertion.cs", "// reviewed assertion");
        fixture.Select("shared");
        fixture.Evaluate(source);
        var input = fixture.Capture().Single(input => input.Path == source);
        Assert.AreEqual("oracle", input.Category);
        Assert.AreEqual(CanonicalJson.FileDigest(source), input.Digest);
    }

    [TestMethod]
    public void ArbitraryImportedTargetsMustBeCaptured()
    {
        using var fixture = new Fixture();
        string import = fixture.File("shared/custom.targets", "<Project/>");
        fixture.Preprocess(import);
        Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
        fixture.Select("shared");
        Assert.IsTrue(fixture.Capture().Any(input => input.Path == import && input.Uses.Contains("import")));
    }

    [TestMethod]
    public void OldObjFilesAreNotTrustedAsFreshGeneratedInputs()
    {
        using var fixture = new Fixture();
        fixture.Evaluate(fixture.File("acceptance/obj/OldAssertion.cs", "// stale generated assertion"));
        Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
        string fresh = fixture.File(".tada/audit/build/GeneratedAssertion.cs", "// fresh generated assertion");
        fixture.Evaluate(fresh);
        Assert.AreEqual("generated", fixture.Capture().Single(input => input.Path == fresh).Category);
    }

    [TestMethod]
    public void AbsentAncestorAnalyzerConfigCandidatesAreNotInputs()
    {
        using var fixture = new Fixture();
        fixture.Evaluate(Path.Combine(fixture.Root, "absent.globalconfig"));
        Assert.IsFalse(fixture.Capture().Any(input => input.Path.EndsWith("absent.globalconfig", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SpoofedSdkImportOutsideSelectedInstallationIsRejected()
    {
        using var fixture = new Fixture();
        fixture.Preprocess(fixture.File("other-sdk/Sdk.targets", "<Project/>"));
        Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
    }

    [TestMethod]
    public void ChangedToolchainInputDuringBuildCannotPassStabilityCheck()
    {
        using var fixture = new Fixture();
        string tool = fixture.File("sdk/Sdk.targets", "<Project/>");
        fixture.Preprocess(tool);
        var before = fixture.Capture();
        File.AppendAllText(tool, "<!-- changed -->");
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.CheckStable(before, fixture.Capture()));
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.CheckCurrent(before));
    }

    [TestMethod]
    public void UnlockedPackageCannotBecomeTrustedThroughPackageFolderProperty()
    {
        using var fixture = new Fixture();
        string source = fixture.File("packages/unlocked/1.0.0/Assertion.dll", "unlocked code");
        fixture.Evaluate(source);
        Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
    }

    [TestMethod]
    public void IntermediateDirectoryOverrideOutsideOwnedRunIsRejected()
    {
        using var fixture = new Fixture();
        fixture.Evaluate(intermediate: fixture.Root);
        var error = Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
        StringAssert.Contains(error.Message, "fresh build directory");
    }

    [TestMethod]
    public void CompilerResponseFilesCannotIntroduceUnenumeratedSettings()
    {
        using var fixture = new Fixture();
        string file = fixture.File("acceptance/Control.rsp", "/define:UNREVIEWED");
        var evaluation = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(fixture.Evaluation))!;
        evaluation["Properties"]!["CompilerResponseFile"] = file;
        File.WriteAllText(fixture.Evaluation, evaluation.ToJsonString());
        var error = Assert.ThrowsExactly<TadaException>(() => fixture.Capture());
        StringAssert.Contains(error.Message, "compiler response files are unsupported");
    }

    [TestMethod]
    public void MissingAndChangedProvenanceReportsCannotVerify()
    {
        using var fixture = new Fixture();
        string run = Path.GetDirectoryName(fixture.Generated)!;
        string evidenceFile = Path.Combine(run, BuildInputs.FileName);
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.Verify(run, fixture.Inventory, null));
        var inputs = fixture.Capture();
        var reports = new[] { fixture.Evaluation, fixture.Preprocessed, fixture.File(".tada/audit/other.json", "{}"), fixture.File(".tada/audit/other.xml", "<Project/>") }
            .ToDictionary(path => Path.GetRelativePath(run, path).Replace('\\', '/'), CanonicalJson.FileDigest);
        Data.WriteNew(evidenceFile, new BuildInputEvidence(1, BuildInputs.Policy, fixture.Sdk,
            [new ProjectBuildInputs("acceptance/Acceptance.csproj", "harness", inputs, inputs, reports)]));
        string digest = CanonicalJson.FileDigest(evidenceFile);
        BuildInputs.Verify(run, fixture.Inventory, digest);
        File.AppendAllText(fixture.Evaluation, " ");
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.Verify(run, fixture.Inventory, digest));
    }

    [TestMethod]
    public void OldBaselineDoesNotAcquireUnrecordedProvenanceByDefault()
    {
        using var fixture = new Fixture();
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.Verify(fixture.Root,
            fixture.Inventory with { SchemaVersion = 1, Policy = new Policy() }, null));
        string json = JsonSerializer.Serialize(new Policy(), Data.Json);
        Assert.IsFalse(json.Contains("buildInputs", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SelectedSdkInstallationComesFromHostInventory()
    {
        using var fixture = new Fixture();
        string sdk = Path.GetDirectoryName(fixture.File(".tada/host/sdk/10.0.403/MSBuild.dll", "synthetic SDK control"))!;
        Assert.AreEqual(sdk, BuildInputs.FindSdk($"10.0.403 [{Path.GetDirectoryName(sdk)}]\r\n", "10.0.403"));
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.FindSdk($"10.0.402 [{Path.GetDirectoryName(sdk)}]\r\n", "10.0.403"));
    }

    [TestMethod]
    public void DifferentObservedIdentitiesForOneToolFileAreNotCollapsed()
    {
        using var fixture = new Fixture();
        string path = fixture.File("sdk/Task.dll", "tool");
        var input = new BuildInput(path, "sdk", CanonicalJson.FileDigest(path), ["Analyzer"]);
        Assert.ThrowsExactly<TadaException>(() => BuildInputs.CheckCurrent([input, input with { Digest = "sha256:" + new string('0', 64) }]));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly CoreTests.Project project = new();
        public string Root => project.Root;
        public string Sdk => Path.Combine(Root, "sdk");
        public string Generated => Path.Combine(Root, ".tada", "audit", "build");
        public string Evaluation => Path.Combine(Root, ".tada", "audit", "evaluation.json");
        public string Preprocessed => Path.Combine(Root, ".tada", "audit", "preprocessed.xml");
        public Inventory Inventory => project.Store.Read(baseline);
        private string baseline;
        public Fixture()
        {
            File("sdk/MSBuild.dll", "synthetic SDK control");
            File("acceptance/packages.lock.json", "{\"version\":1,\"dependencies\":{\"net10.0\":{}}}");
            File(".tada/audit/build/project.assets.json", "{\"libraries\":{},\"packageFolders\":{}}");
            baseline = project.Store.Prepare();
            Evaluate();
            Preprocess();
        }
        public string File(string relative, string text)
        {
            string path = Path.GetFullPath(Path.Combine(Root, relative));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path, text);
            return path;
        }
        public void Select(string root)
        {
            var config = Data.Read<Config>(Path.Combine(Root, "tada.json"));
            System.IO.File.WriteAllText(Path.Combine(Root, "tada.json"), JsonSerializer.Serialize(config with { OracleRoots = [.. config.OracleRoots, root] }, Data.Json));
            baseline = project.Store.Prepare();
        }
        public void Evaluate(string? source = null, string? intermediate = null)
        {
            var properties = new Dictionary<string, string>
            {
                ["MSBuildProjectFullPath"] = Path.Combine(Root, "acceptance", "Acceptance.csproj"),
                ["BaseIntermediateOutputPath"] = intermediate ?? Generated + Path.DirectorySeparatorChar,
                ["IntermediateOutputPath"] = Generated + Path.DirectorySeparatorChar,
                ["MSBuildProjectExtensionsPath"] = Generated + Path.DirectorySeparatorChar,
                ["ProjectAssetsFile"] = Path.Combine(Generated, "project.assets.json")
            };
            File(".tada/audit/evaluation.json", JsonSerializer.Serialize(new
            {
                Properties = properties,
                Items = new { Compile = source == null ? Array.Empty<object>() : [new { FullPath = source }], ProjectReference = Array.Empty<object>() }
            }));
        }
        public void Preprocess(params string[] imports) => File(".tada/audit/preprocessed.xml", "<Project>" +
            string.Join("", imports.Prepend(Path.Combine(Root, "acceptance", "Acceptance.csproj"))
                .Select(path => "<!--\n====================\n" + path + "\n====================\n-->")) + "</Project>");
        public BuildInput[] Capture() => BuildInputs.Capture(new Repository(Root), Inventory,
            Data.Read<Config>(Path.Combine(Root, "tada.json")), Sdk, Generated, Path.Combine(Root, ".tada", "audit", "harness"),
            "harness", "acceptance/Acceptance.csproj", Evaluation, Preprocessed);
        public void Dispose() => project.Dispose();
    }
}
