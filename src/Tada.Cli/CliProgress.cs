using System.Diagnostics;

namespace Tada;

public sealed class CliProgress : IAsyncDisposable
{
    private readonly TextWriter writer;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly object gate = new();
    private readonly Timer timer;
    private string phase = "Starting TADA.";

    public CliProgress(TextWriter writer, TimeSpan? interval = null)
    {
        this.writer = writer;
        TimeSpan period = interval ?? TimeSpan.FromSeconds(10);
        timer = new Timer(_ =>
        {
            lock (gate) Write(phase + " Still running.");
        }, null, period, period);
    }

    public void Report(string message)
    {
        lock (gate)
        {
            phase = CliOutput.SafeText(message);
            Write(phase);
        }
    }

    private void Write(string message)
    {
        try { writer.WriteLine($"[{elapsed.Elapsed:hh\\:mm\\:ss}] {message}"); }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            // A closed diagnostics stream must not interrupt retained evidence collection.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await timer.DisposeAsync();
        elapsed.Stop();
    }
}
