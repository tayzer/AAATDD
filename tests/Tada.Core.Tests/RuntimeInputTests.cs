using System.Text.Json;
using Tada.RuntimeProbe;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class RuntimeInputTests
{
    [TestMethod]
    public void MissingFinalRuntimeSnapshotCannotPassEvenWithConsistentHashes()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => items.Take(items.Length - 1).ToArray());
        AssertBlocked(fixture, id, "complete final snapshot");
    }

    [TestMethod]
    public void UnlocatedManagedAssemblyCannotPassAsOrdinaryRuntimeCode()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => [.. items[..^1], items[1] with { Kind = "unlocated-managed", Path = "", Digest = "", Identity = "memory-only assertion assembly" }, items[^1]]);
        AssertBlocked(fixture, id, "unlocated-managed");
    }

    [TestMethod]
    public void UnidentifiedLoadedFileCannotPassThroughProducerClaims()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        string file = Path.Combine(fixture.Directory(id), "unreviewed.dll");
        File.WriteAllText(file, "unreviewed loaded input");
        Rewrite(fixture.Directory(id), items => [.. items[..^1], items[1] with { Path = file, Digest = CanonicalJson.FileDigest(file) }, items[^1]]);
        AssertBlocked(fixture, id, "not captured in build outputs");
    }

    [TestMethod]
    public void HarnessCannotLoadImplementationAssemblyInProcess()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0), run = fixture.Directory(id);
        string binary = Path.Combine(run, "sut", "Business.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(binary)!);
        File.WriteAllText(binary, "synthetic business assembly");
        string metadata = Path.Combine(run, "run.json");
        var record = Data.Read<RunRecord>(metadata);
        record.Artifacts.Add("sut/Business.dll", CanonicalJson.FileDigest(binary));
        File.WriteAllText(metadata, JsonSerializer.Serialize(record, Data.Json));
        Rewrite(run, items => [.. items[..^1], items[1] with { Path = binary, Digest = CanonicalJson.FileDigest(binary) }, items[^1]]);
        AssertBlocked(fixture, id, "loaded an implementation assembly");
    }

    [TestMethod]
    public void StreamFromAnotherRunCannotBeReboundByEditingCachedDigests()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => items.Select(item => item with { RunId = Guid.NewGuid().ToString("N") }).ToArray());
        AssertBlocked(fixture, id, "identity, sequence or timestamp");
    }

    [TestMethod]
    public void MissingRawRuntimeReportCannotPassOfflineVerification()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Assert.AreEqual("PASS", fixture.Engine.Verify(id).Outcome);
        File.Delete(Directory.GetFiles(Path.Combine(fixture.Directory(id), "runtime"), "*.ndjson").Single());
        AssertBlocked(fixture, id, "report set changed");
    }

    [TestMethod]
    public void WindowsNativeModulePathCasingDoesNotLoseArtifactIdentity()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => [.. items[..^1], items[2] with { Kind = "native", Path = items[2].Path.ToUpperInvariant() }, items[^1]]);
        Assert.AreEqual("PASS", fixture.Engine.Verify(id).Outcome);
    }

    [TestMethod]
    public void MissingNativeLoaderCoverageCannotPassWithFreshCachedDigests()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => items.Where(item => item.Kind != "native-loader-stop").ToArray());
        AssertBlocked(fixture, id, "complete final snapshot");
    }

    [TestMethod]
    public void MissingNativeLoadEventsCannotBeHiddenByClaimingCompletion()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => items.Select(item => item.Kind == "native-loader-stop" ? item with { Identity = "1" } : item).ToArray());
        AssertBlocked(fixture, id, "event count is incomplete");
    }

    [TestMethod]
    public void CapturedTransientNativeLoadHasItsOwnContinuousEventIdentity()
    {
        using var fixture = new HistoryTests.HistoryFixture();
        string id = fixture.Run(0);
        Rewrite(fixture.Directory(id), items => [.. items[..^2], items[2] with { Kind = "native-loaded", Identity = "1" }, items[^2] with { Identity = "1" }, items[^1]]);
        Assert.AreEqual("PASS", fixture.Engine.Verify(id).Outcome);
    }

    [TestMethod]
    public void AmbientCompilerAndRuntimeOverridesAreExcluded()
    {
        string[] names = ["DefineConstants", "MSBuildSDKsPath", "DOTNET_STARTUP_HOOKS", "DOTNET_ADDITIONAL_DEPS", "COR_ENABLE_PROFILING"];
        var originals = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (string name in names) Environment.SetEnvironmentVariable(name, "unreviewed-test-value");
            var captured = ExecutionEnvironment.Capture();
            foreach (string name in names) Assert.IsFalse(captured.ContainsKey(name), name);
            Assert.AreEqual("en-US", captured["DOTNET_CLI_UI_LANGUAGE"]);
        }
        finally { foreach (var item in originals) Environment.SetEnvironmentVariable(item.Key, item.Value); }
    }

    [TestMethod]
    public async Task ReplacedEnvironmentDoesNotLeakAmbientVariablesToChild()
    {
        using var project = new CoreTests.Project();
        const string name = "TADA_ENVIRONMENT_LEAK_CONTROL";
        string? original = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, "UNREVIEWED_CONTROL");
            // Use shell built-ins with AutoRun disabled. Windows PowerShell's startup/module
            // discovery under a scrubbed environment is unrelated to this isolation boundary.
            string shell = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
            string[] arguments = ["/d", "/s", "/c", "if defined TADA_ENVIRONMENT_LEAK_CONTROL (exit /b 42) else (echo clean)"];
            var inherited = await Processes.Execute(shell, arguments, project.Root, 10);
            Assert.IsFalse(inherited.TimedOut, inherited.Stderr);
            Assert.AreEqual(42, inherited.ExitCode, "The positive control did not see the ambient variable.");
            var result = await Processes.Execute(shell, arguments, project.Root, 10,
                ExecutionEnvironment.Capture(), replaceEnvironment: true);
            Assert.IsFalse(result.TimedOut, result.Stderr);
            Assert.AreEqual(0, result.ExitCode, result.Stderr);
            StringAssert.Contains(result.Stdout, "clean");
        }
        finally { Environment.SetEnvironmentVariable(name, original); }
    }

    private static void AssertBlocked(HistoryTests.HistoryFixture fixture, string id, string detail)
    {
        var result = fixture.Engine.Verify(id);
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(reason => reason.Detail.Contains(detail, StringComparison.Ordinal)), JsonSerializer.Serialize(result, Data.Json));
    }

    private static void Rewrite(string run, Func<Observation[], Observation[]> change)
    {
        string stream = Directory.GetFiles(Path.Combine(run, "runtime"), "*.ndjson").Single();
        var items = File.ReadLines(stream).Select(line => JsonSerializer.Deserialize<Observation>(line, Data.Json)!).ToArray();
        File.WriteAllLines(stream, change(items).Select((item, index) => JsonSerializer.Serialize(item with { Sequence = index + 1 }, Data.Json).Replace("\r", "").Replace("\n", "")));
        string evidencePath = Path.Combine(run, RuntimeInputs.FileName);
        var evidence = Data.Read<RuntimeInputEvidence>(evidencePath);
        evidence.Reports[Path.GetRelativePath(run, stream).Replace('\\', '/')] = CanonicalJson.FileDigest(stream);
        File.WriteAllText(evidencePath, JsonSerializer.Serialize(evidence, Data.Json));
        string metadata = Path.Combine(run, "run.json");
        var record = Data.Read<RunRecord>(metadata) with { RuntimeInputsDigest = CanonicalJson.FileDigest(evidencePath) };
        File.WriteAllText(metadata, JsonSerializer.Serialize(record, Data.Json));
    }
}
