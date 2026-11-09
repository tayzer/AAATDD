using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class ProcessTests
{
    [TestMethod]
    public async Task CancellationRetainsDiagnosticsAndTerminatesOwnedChild()
    {
        using var project = new CoreTests.Project();
        using var cancellation = new CancellationTokenSource();
        string ready = Path.Combine(project.Root, "child.pid");
        Task<CommandEvidence> execution = ExecuteTree(project.Root, ready, 30, cancellation.Token);
        using Process child = await Child(ready, execution);
        try
        {
            cancellation.Cancel();
            CommandEvidence result = await execution.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(result.Cancelled);
            Assert.IsFalse(result.TimedOut);
            Assert.AreNotEqual(0, result.ExitCode);
            StringAssert.Contains(result.Stdout, "CONTROL_STARTED");
            StringAssert.Contains(result.Stderr, "CONTROL_DIAGNOSTIC");
            Assert.IsTrue(child.WaitForExit(5000), "Owned child survived cancellation.");
        }
        finally
        {
            cancellation.Cancel();
            if (!child.HasExited) child.Kill(entireProcessTree: true);
        }
    }

    [TestMethod]
    public async Task DeadlineRetainsDiagnosticsAndTerminatesOwnedChild()
    {
        using var project = new CoreTests.Project();
        string ready = Path.Combine(project.Root, "child.pid");
        Task<CommandEvidence> execution = ExecuteTree(project.Root, ready, 4);
        using Process child = await Child(ready, execution);
        try
        {
            CommandEvidence result = await execution.WaitAsync(TimeSpan.FromSeconds(12));
            Assert.IsTrue(result.TimedOut);
            Assert.IsFalse(result.Cancelled);
            StringAssert.Contains(result.Stdout, "CONTROL_STARTED");
            StringAssert.Contains(result.Stderr, "CONTROL_DIAGNOSTIC");
            Assert.IsTrue(child.WaitForExit(5000), "Owned child survived the deadline.");
        }
        finally { if (!child.HasExited) child.Kill(entireProcessTree: true); }
    }

    [TestMethod]
    public async Task AlreadyCancelledOperationDoesNotLaunchAProcess()
    {
        using var project = new CoreTests.Project();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        string ready = Path.Combine(project.Root, "unexpected.txt");
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Processes.Execute("powershell.exe",
            ["-NoProfile", "-NonInteractive", "-Command", "[IO.File]::WriteAllText($env:TADA_PROCESS_CONTROL, 'unexpected')"],
            project.Root, 10, new Dictionary<string, string> { ["TADA_PROCESS_CONTROL"] = ready }, cancellation.Token));
        Assert.IsFalse(File.Exists(ready), "A cancelled operation launched its command.");
    }

    private static Task<CommandEvidence> ExecuteTree(string directory, string ready, int timeout, CancellationToken cancellation = default) =>
        Processes.Execute("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", """
            $child = Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile','-NonInteractive','-Command','Start-Sleep -Seconds 40' -PassThru
            [Console]::Out.WriteLine('CONTROL_STARTED')
            [Console]::Error.WriteLine('CONTROL_DIAGNOSTIC')
            [Console]::Out.Flush()
            [Console]::Error.Flush()
            [IO.File]::WriteAllText($env:TADA_PROCESS_CONTROL + '.tmp', [string]$child.Id)
            [IO.File]::Move($env:TADA_PROCESS_CONTROL + '.tmp', $env:TADA_PROCESS_CONTROL)
            Start-Sleep -Seconds 40
            """], directory, timeout, new Dictionary<string, string> { ["TADA_PROCESS_CONTROL"] = ready }, cancellation);

    private static async Task<Process> Child(string ready, Task<CommandEvidence> execution)
    {
        var deadline = Stopwatch.StartNew();
        while (!File.Exists(ready) && !execution.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(25);
        Assert.IsTrue(File.Exists(ready), "Process-tree control did not signal readiness.");
        return Process.GetProcessById(int.Parse(File.ReadAllText(ready), System.Globalization.CultureInfo.InvariantCulture));
    }
}
