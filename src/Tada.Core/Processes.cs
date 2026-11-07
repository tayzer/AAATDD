using System.Diagnostics;

namespace Tada;

public static class Processes
{
    public static async Task<CommandEvidence> Execute(string executable, string[] arguments, string directory,
        int timeoutSeconds, IReadOnlyDictionary<string, string>? environment = null, CancellationToken cancellation = default,
        bool replaceEnvironment = false)
    {
        cancellation.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory, UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        if (replaceEnvironment) start.Environment.Clear();
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false";
        if (environment != null)
            foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        using var process = new Process { StartInfo = start };
        DateTimeOffset began = DateTimeOffset.UtcNow;
        process.Start();
        // Cancellation stops the owned process tree, not collection of its retained diagnostics.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        bool timeout = false, cancelled = false;
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            cancelled = cancellation.IsCancellationRequested;
            timeout = !cancelled;
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
        return new CommandEvidence(executable, arguments, process.ExitCode, timeout, began,
            DateTimeOffset.UtcNow, await stdout, await stderr, cancelled);
    }
}
