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
        while (true)
        {
            try
            {
                if (File.Exists(acknowledgement))
                {
                    if (File.ReadAllText(acknowledgement) != run) throw new IOException("API runtime checkpoint belongs to another run.");
                    return;
                }
            }
            catch (IOException error) when ((error.HResult & 65535) is 32 or 33)
            {
                // A transient Windows sharing conflict still counts toward the checkpoint deadline.
            }
            if (process.HasExited) throw new IOException("API exited before its runtime checkpoint completed.");
            if (deadline.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("API runtime checkpoint did not complete.");
            Thread.Sleep(10);
        }
    }
}
