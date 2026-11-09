using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class CliTests
{
    [TestMethod]
    public async Task RunReportsProgressWhileMachineOutputRemainsOneJsonDocument()
    {
        using var project = new CoreTests.Project();
        string baseline = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(baseline).Id, "automated progress control; not human acceptance");
        File.AppendAllText(project.Feature, "\n# unapproved progress control\n");
        var readable = await Invoke(project.Root, "run", "--baseline", baseline);
        Assert.AreEqual(2, readable.ExitCode);
        StringAssert.Contains(readable.Stderr, "Validating baseline approval and current inputs.");
        StringAssert.Contains(readable.Stderr, "Saving retained run evidence.");
        StringAssert.Contains(readable.Stdout, "INDETERMINATE");
        var machine = await Invoke(project.Root, "run", "--baseline", baseline, "--json");
        Assert.AreEqual(2, machine.ExitCode);
        Assert.AreEqual("", machine.Stderr);
        using var json = JsonDocument.Parse(machine.Stdout);
        Assert.AreEqual("INDETERMINATE", json.RootElement.GetProperty("outcome").GetString());
    }

    [TestMethod]
    public async Task ProgressRepeatsTheActivePhaseAndStopsBeforeFinalOutput()
    {
        using var writer = new ProgressWriter();
        await using (var progress = new CliProgress(writer, TimeSpan.FromMilliseconds(20)))
        {
            progress.Report("Restoring dependencies.\u001b[2J");
            await writer.Heartbeat.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        string finished = writer.ToString();
        StringAssert.Contains(finished, "Restoring dependencies.\\u001b[2J Still running.");
        await Task.Delay(60);
        Assert.AreEqual(finished, writer.ToString(), "Progress continued after disposal.");
    }

    private sealed class ProgressWriter : StringWriter
    {
        public TaskCompletionSource Heartbeat { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override void WriteLine(string? value)
        {
            base.WriteLine(value);
            if (value?.Contains("Still running.", StringComparison.Ordinal) == true) Heartbeat.TrySetResult();
        }
    }

    [TestMethod]
    public async Task InitPreparesTheProjectAndLeavesActivationToThePlugin()
    {
        using var project = new CoreTests.Project();
        string instructions = Path.Combine(project.Root, "AGENTS.md");
        File.WriteAllText(instructions, "Existing project conventions.");
        var first = await Invoke(project.Root, "init", "--json");
        Assert.AreEqual(0, first.ExitCode, first.Stderr);
        using var result = JsonDocument.Parse(first.Stdout);
        Assert.AreEqual(0, result.RootElement.GetProperty("updated").GetArrayLength());
        Assert.AreEqual("Existing project conventions.", File.ReadAllText(instructions));
        StringAssert.Contains(result.RootElement.GetProperty("next").GetString()!, "Enable the TADA plugin");
        StringAssert.Contains(result.RootElement.GetProperty("next").GetString()!, "describe what you want to build or change normally");
        string context = File.ReadAllText(Path.Combine(project.Root, ".tada", "context.md"));
        StringAssert.Contains(context, "## Local SDK host");
        StringAssert.Contains(context, "--dotnet");
        var repeat = await Invoke(project.Root, "init", "--json");
        Assert.AreEqual(0, repeat.ExitCode, repeat.Stderr);
        using var repeated = JsonDocument.Parse(repeat.Stdout);
        Assert.AreEqual(0, repeated.RootElement.GetProperty("created").GetArrayLength());
        Assert.AreEqual(0, repeated.RootElement.GetProperty("updated").GetArrayLength());
    }

    [TestMethod]
    public async Task ReviewShowsActualAcceptanceTextAndSupportsExplicitMachineOutput()
    {
        using var project = new CoreTests.Project();
        string baseline = project.Store.Prepare();
        var readable = await Invoke(project.Root, "baseline", "review", "--baseline", baseline);
        Assert.AreEqual(0, readable.ExitCode, readable.Stderr);
        StringAssert.Contains(readable.Stdout, CoreTests.Project.FeatureText);
        StringAssert.Contains(readable.Stdout, "This is a review record, not approval.");
        StringAssert.Contains(readable.Stdout, "Build-input policy: " + BuildInputs.Policy);
        StringAssert.Contains(readable.Stdout, "Task policy: " + TaskJournal.Policy);
        StringAssert.Contains(readable.Stdout, "Next: tada baseline approve --review");
        var machine = await Invoke(project.Root, "baseline", "review", "--baseline", baseline, "--json");
        Assert.AreEqual(0, machine.ExitCode, machine.Stderr);
        using var json = JsonDocument.Parse(machine.Stdout);
        Assert.AreEqual(baseline, json.RootElement.GetProperty("review").GetProperty("baseline").GetString());
        Assert.AreEqual(CoreTests.Project.FeatureText, json.RootElement.GetProperty("specifications")[0].GetProperty("content").GetString());
    }

    [TestMethod]
    public async Task IncompleteCommandHasReadableAndMachineErrorsWithoutClaimingVerification()
    {
        using var project = new CoreTests.Project();
        var readable = await Invoke(project.Root, "run");
        Assert.AreEqual(3, readable.ExitCode);
        StringAssert.Contains(readable.Stderr, "Explicit --baseline is required.");
        var machine = await Invoke(project.Root, "run", "--json");
        Assert.AreEqual(3, machine.ExitCode);
        using var json = JsonDocument.Parse(machine.Stderr);
        Assert.AreEqual(3, json.RootElement.GetProperty("exitCode").GetInt32());
        Assert.IsFalse(json.RootElement.TryGetProperty("outcome", out _));
    }

    [TestMethod]
    public void BlockingResultNamesTheLogicalCaseAndReason()
    {
        var result = new Verification("INDETERMINATE", "L1-advisory", "sha256:example", "run-example", [],
            [new Reason("missing-case", "The required case did not execute.", "AC_EXAMPLE")]);
        string rendered = CliOutput.Render(JsonSerializer.SerializeToElement(result, Data.Json));
        StringAssert.Contains(rendered, "INDETERMINATE");
        StringAssert.Contains(rendered, "missing-case [AC_EXAMPLE]");
        StringAssert.Contains(rendered, "Completion is blocked.");
        StringAssert.Contains(rendered, "not protected independent assurance");
    }

    [TestMethod]
    public async Task FlagCannotBeConsumedAsAnApproverDeclaration()
    {
        using var project = new CoreTests.Project();
        var review = project.Store.Review(project.Store.Prepare());
        var result = await Invoke(project.Root, "baseline", "approve", "--review", review.Id, "--by", "--json");
        Assert.AreEqual(3, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stderr);
        StringAssert.Contains(json.RootElement.GetProperty("error").GetString()!, "Missing value for --by");
        string approvals = Path.Combine(project.Root, ".tada", "approvals");
        Assert.IsFalse(Directory.Exists(approvals) && Directory.EnumerateFiles(approvals).Any());
    }

    [TestMethod]
    public void UntrustedDiagnosticsCannotEraseOrSpoofTerminalOutput()
    {
        string output = CliOutput.SafeText("FAIL\r\nreason: \u001b[2J\rPASS\b\u202e");
        Assert.AreEqual("FAIL\nreason: \\u001b[2J\\u000dPASS\\u0008\\u202e", output);
    }

    private static Task<CommandEvidence> Invoke(string repository, params string[] arguments)
    {
        DirectoryInfo? source = new(AppContext.BaseDirectory);
        while (source != null && !Directory.Exists(Path.Combine(source.FullName, "src", "Tada.Cli"))) source = source.Parent;
        if (source == null) throw new InvalidOperationException("Cannot locate built CLI source for black-box tests.");
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string cli = Path.Combine(source.FullName, "src", "Tada.Cli", "bin", configuration, "net10.0", "Tada.Cli.dll");
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
        if (!File.Exists(host)) host = "dotnet";
        return Processes.Execute(host, [cli, .. arguments, "--repo", repository], repository, 30);
    }
}
