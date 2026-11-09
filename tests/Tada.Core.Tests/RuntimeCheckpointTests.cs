using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tada.RuntimeProbe;

namespace Tada.Tests;

[TestClass, DoNotParallelize]
public sealed class RuntimeCheckpointTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HelperWaitsForAcknowledgementSharingConflictAndChecksIdentity(bool wrongIdentity)
    {
        using var project = new CoreTests.Project();
        using var process = Process.GetCurrentProcess();
        string? previousDirectory = Environment.GetEnvironmentVariable("TADA_RUNTIME_DIRECTORY");
        string? previousRun = Environment.GetEnvironmentVariable("TADA_RUN_ID");
        string run = Guid.NewGuid().ToString("N");
        string request = Path.Combine(project.Root, $"{process.Id}.checkpoint-request");
        string acknowledgement = Path.Combine(project.Root, $"{process.Id}.checkpoint-complete");
        try
        {
            Environment.SetEnvironmentVariable("TADA_RUNTIME_DIRECTORY", project.Root);
            Environment.SetEnvironmentVariable("TADA_RUN_ID", run);
            Task completion;
            using (var writer = new FileStream(acknowledgement, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                writer.Write(Encoding.UTF8.GetBytes(wrongIdentity ? Guid.NewGuid().ToString("N") : run));
                writer.Flush();
                completion = Task.Run(() => TadaRuntimeCheckpoint.Complete(process));
                Assert.IsTrue(SpinWait.SpinUntil(() => File.Exists(request), TimeSpan.FromSeconds(10)));
                Thread.Sleep(100);
            }
            if (wrongIdentity)
            {
                var error = await Assert.ThrowsExactlyAsync<IOException>(() => completion);
                StringAssert.Contains(error.Message, "belongs to another run");
            }
            else await completion;
        }
        finally
        {
            Environment.SetEnvironmentVariable("TADA_RUNTIME_DIRECTORY", previousDirectory);
            Environment.SetEnvironmentVariable("TADA_RUN_ID", previousRun);
        }
    }

    [TestMethod]
    public void CheckpointWaitsForLegacyRequestWriterBeforeCompletingObservation()
    {
        using var project = new CoreTests.Project();
        string? previousDirectory = Environment.GetEnvironmentVariable("TADA_RUNTIME_DIRECTORY");
        string? previousRun = Environment.GetEnvironmentVariable("TADA_RUN_ID");
        string run = Guid.NewGuid().ToString("N");
        string request = Path.Combine(project.Root, $"{Environment.ProcessId}.checkpoint-request");
        string acknowledgement = Path.Combine(project.Root, $"{Environment.ProcessId}.checkpoint-complete");
        try
        {
            Environment.SetEnvironmentVariable("TADA_RUNTIME_DIRECTORY", project.Root);
            Environment.SetEnvironmentVariable("TADA_RUN_ID", run);
            using (var writer = new FileStream(request, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                writer.Write(Encoding.UTF8.GetBytes(run));
                writer.Flush();
                Observer.Start();
                // Legacy helpers expose the request while their write handle is still open.
                Thread.Sleep(300);
                Assert.IsFalse(File.Exists(acknowledgement));
            }
            Assert.IsTrue(SpinWait.SpinUntil(() => File.Exists(acknowledgement), TimeSpan.FromSeconds(10)),
                "Observer did not complete after the request writer released its handle.");
            Assert.AreEqual(run, File.ReadAllText(acknowledgement));
            string report = File.ReadAllText(Directory.GetFiles(project.Root, "*.ndjson").Single());
            Assert.IsFalse(report.Contains("\"kind\":\"error\"", StringComparison.Ordinal),
                "A transient checkpoint sharing violation must not invalidate runtime observation.");
            StringAssert.Contains(report, "\"kind\":\"checkpoint\"");
            StringAssert.Contains(report, "\"identity\":\"complete\"");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TADA_RUNTIME_DIRECTORY", previousDirectory);
            Environment.SetEnvironmentVariable("TADA_RUN_ID", previousRun);
        }
    }
}
