using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

/// <summary>Synthetic run records around actual Reqnroll Messages exercise history, not producer authenticity.</summary>
[TestClass]
public sealed class HistoryTests
{
    [TestMethod]
    public void PassingRerunDoesNotDiscardNativeFailureEvenWithoutCachedResult()
    {
        using var fixture = new HistoryFixture();
        fixture.Run(0, failure: true);
        string next = fixture.Run(1);
        var result = fixture.Engine.Verify(next);
        Assert.AreEqual("FAIL", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "unresolved-earlier-failure"));
    }

    [TestMethod]
    public void EditedCachedSuccessCannotEraseEarlierFailure()
    {
        using var fixture = new HistoryFixture();
        string failed = fixture.Run(0, failure: true);
        Data.WriteNew(Path.Combine(fixture.Directory(failed), "result.json"),
            new Verification("PASS", "L1-advisory", fixture.Baseline, failed, [], []));
        Assert.AreEqual("FAIL", fixture.Engine.Verify(fixture.Run(1)).Outcome);
    }

    [TestMethod]
    public void MissingEarlierNativeEvidenceBlocksPass()
    {
        using var fixture = new HistoryFixture();
        string previous = fixture.Run(0, failure: true);
        File.Delete(Path.Combine(fixture.Directory(previous), "harness", "messages.ndjson"));
        var result = fixture.Engine.Verify(fixture.Run(1));
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "history-integrity"));
    }

    [TestMethod]
    public void TrustworthyEarlierFailureSurvivesAnotherHistoryGap()
    {
        using var fixture = new HistoryFixture();
        fixture.Run(0, failure: true);
        fixture.Interrupted(1);
        var result = fixture.Engine.Verify(fixture.Run(2));
        Assert.AreEqual("FAIL", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "history-gap"));
    }

    [TestMethod]
    public void InterruptedEarlierAttemptCannotSilentlyDisappear()
    {
        using var fixture = new HistoryFixture();
        fixture.Interrupted(0);
        Assert.AreEqual("INDETERMINATE", fixture.Engine.Verify(fixture.Run(1)).Outcome);
    }

    [TestMethod]
    public void IdentifiedImplementationFixCanResolveTheEarlierFailure()
    {
        using var fixture = new HistoryFixture();
        fixture.Run(0, failure: true);
        Assert.AreEqual("PASS", fixture.Engine.Verify(fixture.Run(1, implementation: "fixed")).Outcome);
    }

    [TestMethod]
    public void CompletedSetupGapCanBeResolvedByValidFreshEvidence()
    {
        using var fixture = new HistoryFixture();
        fixture.Run(0, setupGap: true);
        Assert.AreEqual("PASS", fixture.Engine.Verify(fixture.Run(1)).Outcome);
    }

    [TestMethod]
    public void NonBinaryOracleResourcesAreProtectedByArtifactIntegrity()
    {
        using var fixture = new HistoryFixture();
        string run = fixture.Run(0);
        string resource = Path.Combine(fixture.Directory(run), "harness", "expected.csv");
        File.WriteAllText(resource, "expected,2\n");
        string recordFile = Path.Combine(fixture.Directory(run), "run.json");
        var record = Data.Read<RunRecord>(recordFile);
        record.Artifacts.Add("harness/expected.csv", CanonicalJson.FileDigest(resource));
        File.WriteAllText(recordFile, System.Text.Json.JsonSerializer.Serialize(record, Data.Json));
        Assert.AreEqual("PASS", fixture.Engine.Verify(run).Outcome);
        File.WriteAllText(resource, "expected,3\n");
        Assert.AreEqual("INDETERMINATE", fixture.Engine.Verify(run).Outcome);
    }

    [TestMethod]
    public void UnrecordedAddedRuntimeResourcesCannotPass()
    {
        using var fixture = new HistoryFixture();
        string run = fixture.Run(0);
        Assert.AreEqual("PASS", fixture.Engine.Verify(run).Outcome);
        File.WriteAllText(Path.Combine(fixture.Directory(run), "harness", "unexpected.csv"), "unexpected,3\n");
        Assert.AreEqual("INDETERMINATE", fixture.Engine.Verify(run).Outcome);
    }

    [TestMethod]
    public void CancelledCommandCannotUseCompleteSuccessfulMessages()
    {
        using var fixture = new HistoryFixture();
        string run = fixture.Run(0);
        string path = Path.Combine(fixture.Directory(run), "run.json");
        var record = Data.Read<RunRecord>(path);
        record.Commands[^1] = record.Commands[^1] with { Cancelled = true };
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(record, Data.Json));
        var result = fixture.Engine.Verify(run);
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(reason => reason.Detail.Contains("cancelled", StringComparison.OrdinalIgnoreCase)));
    }

    internal sealed class HistoryFixture : IDisposable
    {
        private readonly CoreTests.Project project = new();
        private readonly DateTimeOffset epoch = DateTimeOffset.UtcNow.AddMinutes(-20);
        private readonly Approval approval;
        public string Baseline { get; }
        public RunEngine Engine => new(project.Store);
        public HistoryFixture()
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "orders.feature"), project.Feature, true);
            Baseline = project.Store.Prepare();
            var review = new Review(Guid.NewGuid().ToString("N"), Baseline, epoch.AddMinutes(-2), null);
            approval = new Approval(Guid.NewGuid().ToString("N"), review.Id, Baseline, "synthetic history control", epoch.AddMinutes(-1), null);
            Data.WriteNew(project.Store.RecordPath("reviews", review.Id), review);
            Data.WriteNew(project.Store.RecordPath("approvals", approval.Id), approval);
        }
        public string Directory(string id) => Path.Combine(project.Root, ".tada", "runs", id);
        public void Interrupted(int index)
        {
            string id = Guid.NewGuid().ToString("N");
            Data.WriteNew(Path.Combine(Directory(id), "intent.json"), new RunIntent(id, Baseline, epoch.AddMinutes(index)));
        }
        public string Run(int index, bool failure = false, bool setupGap = false, string implementation = "same")
        {
            string id = Guid.NewGuid().ToString("N");
            string directory = Directory(id);
            string harness = Path.Combine(directory, "harness");
            System.IO.Directory.CreateDirectory(harness);
            string binary = Path.Combine(harness, "Acceptance.dll");
            File.WriteAllText(binary, "synthetic artifact identity control");
            var events = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "orders-pass.ndjson"))
                .Select(line => JsonNode.Parse(line)!.AsObject()).ToArray();
            DateTimeOffset started = epoch.AddMinutes(index);
            DateTimeOffset finished = started.AddSeconds(20);
            events.Single(e => e.ContainsKey("testRunStarted"))["testRunStarted"]!["timestamp"]!["seconds"] = started.ToUnixTimeSeconds();
            events.Single(e => e.ContainsKey("testRunFinished"))["testRunFinished"]!["timestamp"]!["seconds"] = finished.ToUnixTimeSeconds();
            if (failure || setupGap)
            {
                JsonNode result;
                if (setupGap) result = events.First(e => e.ContainsKey("testStepFinished"))["testStepFinished"]!["testStepResult"]!;
                else
                {
                    string pickleStep = events.First(e => e.ContainsKey("pickle"))["pickle"]!["steps"]!.AsArray()
                        .First(s => s!["type"]!.GetValue<string>() == "Outcome")!["id"]!.GetValue<string>();
                    string testStep = events.Where(e => e.ContainsKey("testCase")).SelectMany(e => e["testCase"]!["testSteps"]!.AsArray())
                        .First(s => s!["pickleStepId"]?.GetValue<string>() == pickleStep)!["id"]!.GetValue<string>();
                    result = events.First(e => e["testStepFinished"]?["testStepId"]?.GetValue<string>() == testStep)["testStepFinished"]!["testStepResult"]!;
                }
                result["status"] = "FAILED";
                result["message"] = "synthetic history sensitivity control";
                result["exception"] = new JsonObject { ["type"] = "AssertFailedException" };
                events.Single(e => e.ContainsKey("testRunFinished"))["testRunFinished"]!["success"] = false;
            }
            string messages = Path.Combine(harness, "messages.ndjson");
            File.WriteAllLines(messages, events.Select(e => e.ToJsonString()));
            var command = new CommandEvidence("dotnet", [binary], failure || setupGap ? 2 : 0, false, started, finished, "", "");
            var record = new RunRecord(id, Baseline, approval.Id, started, finished,
                CanonicalJson.Digest(Encoding.UTF8.GetBytes(implementation)), "environment-control", CanonicalJson.FileDigest(messages),
                new Dictionary<string, string> { ["harness/Acceptance.dll"] = CanonicalJson.FileDigest(binary) }, [command], []);
            // Synthetic producer evidence keeps these tests focused on history. Real MSBuild
            // provenance is exercised by the Orders and installed-package integration controls.
            var inputs = project.Store.Read(Baseline).Files.Select(file => new BuildInput(
                Path.Combine(project.Root, file.Path), "oracle", file.Digest, ["synthetic-history-control"])).ToArray();
            var reports = new Dictionary<string, string>();
            foreach (string name in new[] { "before.json", "before.xml", "after.json", "after.xml" })
            {
                string report = Path.Combine(directory, name);
                File.WriteAllText(report, name.EndsWith(".json", StringComparison.Ordinal)
                    ? "{\"Properties\":{\"TadaVerifiedBuild\":\"true\"},\"Items\":{\"ProjectReference\":[]}}" : "synthetic history producer control");
                reports.Add(name, CanonicalJson.FileDigest(report));
            }
            string evidence = Path.Combine(directory, BuildInputs.FileName);
            Data.WriteNew(evidence, new BuildInputEvidence(2, BuildInputs.Policy, "synthetic-sdk",
                [new ProjectBuildInputs("acceptance/Acceptance.csproj", "harness", inputs, inputs, reports, [])]));
            record = record with { BuildInputsDigest = CanonicalJson.FileDigest(evidence) };
            string observer = Path.Combine(directory, RuntimeInputs.ProbePath);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(observer)!);
            File.WriteAllText(observer, "synthetic runtime observer identity control");
            record.Artifacts.Add(RuntimeInputs.ProbePath, CanonicalJson.FileDigest(observer));
            string nativeObserver = Path.Combine(directory, RuntimeInputs.NativeProbePath);
            File.WriteAllText(nativeObserver, "synthetic native observer identity control");
            record.Artifacts.Add(RuntimeInputs.NativeProbePath, CanonicalJson.FileDigest(nativeObserver));
            string environment = Path.Combine(directory, RuntimeInputs.EnvironmentFile);
            Data.WriteNew(environment, new Dictionary<string, string>());
            string runtime = Path.Combine(directory, "runtime");
            System.IO.Directory.CreateDirectory(runtime);
            string stream = Path.Combine(runtime, "1234-control.ndjson");
            string processPath = Path.GetFullPath(binary);
            var observations = new[]
            {
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 1, started, "start", processPath, "", RuntimeInputs.Policy),
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 2, started, "managed", processPath, CanonicalJson.FileDigest(binary), "synthetic harness"),
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 3, started, "managed", observer, CanonicalJson.FileDigest(observer), "synthetic observer"),
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 4, started, "native-loader-start", nativeObserver, CanonicalJson.FileDigest(nativeObserver), "1"),
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 5, started, "native-loader-stop", "", "", "0"),
                new RuntimeProbe.Observation(1, id, 1234, started.UtcTicks, 6, finished, "exit", "", "", "complete")
            };
            File.WriteAllLines(stream, observations.Select(item => System.Text.Json.JsonSerializer.Serialize(item, Data.Json).Replace("\r", "").Replace("\n", "")));
            string runtimeEvidence = Path.Combine(directory, RuntimeInputs.FileName);
            Data.WriteNew(runtimeEvidence, new RuntimeInputEvidence(2, RuntimeInputs.Policy, Path.Combine(project.Root, "syntheticSystem"),
                new Dictionary<string, string> { [Path.Combine(project.Root, "syntheticPlatform.dll")] = CanonicalJson.FileDigest(observer) },
                new Dictionary<string, string> { ["runtime/1234-control.ndjson"] = CanonicalJson.FileDigest(stream) }));
            record = record with { RuntimeInputsDigest = CanonicalJson.FileDigest(runtimeEvidence), ExecutionEnvironmentDigest = CanonicalJson.FileDigest(environment) };
            string logger = Path.GetFullPath(Path.Combine(directory, TaskJournal.LoggerPath));
            File.WriteAllText(logger, "synthetic task logger identity control");
            record.Artifacts.Add(TaskJournal.LoggerPath, CanonicalJson.FileDigest(logger));
            string taskAssembly = Path.Combine(project.Root, "sdk", "Microsoft.Build.Tasks.Core.dll");
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(taskAssembly)!);
            File.WriteAllText(taskAssembly, "synthetic SDK task identity control");
            string origin = Path.Combine(project.Root, "acceptance", "Acceptance.csproj");
            var taskStart = new BuildLogger.TaskEvent(1, 2, started, "task-start", "control-task", origin, origin, "Build", "Message", taskAssembly,
                CanonicalJson.FileDigest(taskAssembly), "", [], true);
            var taskRows = new[]
            {
                taskStart with { Sequence = 1, Kind = "logger-start", Key = "" }, taskStart,
                taskStart with { Sequence = 3, Kind = "task-finish" }, taskStart with { Sequence = 4, Kind = "logger-finish", Key = "" }
            };
            string taskStream = Path.Combine(directory, "build", "control.tasks.ndjson");
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(taskStream)!);
            File.WriteAllLines(taskStream, taskRows.Select(item => System.Text.Json.JsonSerializer.Serialize(item, Data.Json).Replace("\r", "").Replace("\n", "")));
            string taskEvidence = Path.Combine(directory, TaskJournal.FileName);
            Data.WriteNew(taskEvidence, new TaskInputEvidence(1, TaskJournal.Policy, project.Root, Path.Combine(project.Root, "sdk"),
                [new TaskReport("build/control.tasks.ndjson", CanonicalJson.FileDigest(taskStream), "harness", "build", started, started)],
                [new BuildInput(origin, "oracle", CanonicalJson.FileDigest(origin), ["task-origin"]), new BuildInput(taskAssembly, "sdk", CanonicalJson.FileDigest(taskAssembly), ["task-assembly"])]));
            record = record with
            {
                TaskInputsDigest = CanonicalJson.FileDigest(taskEvidence),
                Commands = [new CommandEvidence("dotnet", ["msbuild", "acceptance/Acceptance.csproj", "-t:Rebuild", "-logger:" + logger + ";" + taskStream], 0, false, started, started, "", ""), command]
            };
            Data.WriteNew(Path.Combine(directory, "intent.json"), new RunIntent(id, Baseline, started));
            Data.WriteNew(Path.Combine(directory, "run.json"), record);
            return id;
        }
        public void Dispose() => project.Dispose();
    }
}
