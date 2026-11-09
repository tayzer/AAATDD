using System.Diagnostics;

// Cooperate with TADA's injected observer without referencing application or verifier assemblies.
public static class TadaRuntimeCheckpoint
{
    public static void Complete(Process process)
    {
        string? directory = Environment.GetEnvironmentVariable("TADA_RUNTIME_DIRECTORY");
        if (directory == null || process.HasExited) return;
        string run = Environment.GetEnvironmentVariable("TADA_RUN_ID") ?? throw new InvalidOperationException("TADA run identity is missing.");
        string acknowledgement = Path.Combine(directory, $"{process.Id}.checkpoint-complete");
        File.WriteAllText(Path.Combine(directory, $"{process.Id}.checkpoint-request"), run);
        var deadline = Stopwatch.StartNew();
        while (!File.Exists(acknowledgement))
        {
            if (process.HasExited) throw new IOException("API exited before its runtime checkpoint completed.");
            if (deadline.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("API runtime checkpoint did not complete.");
            Thread.Sleep(10);
        }
        if (File.ReadAllText(acknowledgement) != run) throw new IOException("API runtime checkpoint belongs to another run.");
    }
}
