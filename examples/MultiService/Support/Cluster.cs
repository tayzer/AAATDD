using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public sealed class Cluster : IAsyncDisposable
{
    private readonly List<Process> processes = [];
    private readonly List<Task> diagnostics = [];
    private readonly List<TcpListener> listeners = [];
    private readonly TadaAcceptance context = TadaAcceptance.Create();
    public HttpClient Http { get; } = new() { Timeout = TimeSpan.FromSeconds(3) };
    public Dictionary<string, string> Endpoints { get; } = [];

    public async Task Start()
    {
        try
        {
            foreach (string id in context.Services.Keys)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                listeners.Add(listener);
                Endpoints.Add(id, "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port);
            }
            foreach (var service in context.Services)
            {
                listeners[processes.Count].Stop();
                var start = new ProcessStartInfo(context.DotnetHost)
                {
                    WorkingDirectory = context.WorkingDirectory, UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                foreach (string value in new[] { service.Value, "--role", service.Key, "--urls", Endpoints[service.Key],
                    "--database", Path.Combine(context.WorkingDirectory, service.Key + ".sqlite") }) start.ArgumentList.Add(value);
                foreach (var endpoint in Endpoints)
                {
                    start.ArgumentList.Add("--" + endpoint.Key);
                    start.ArgumentList.Add(endpoint.Value);
                }
                var process = Process.Start(start) ?? throw new InvalidOperationException("Service process did not start.");
                processes.Add(process);
                diagnostics.Add(Save(process.StandardOutput, Path.Combine(context.WorkingDirectory, service.Key + ".stdout.log")));
                diagnostics.Add(Save(process.StandardError, Path.Combine(context.WorkingDirectory, service.Key + ".stderr.log")));
            }
            foreach (var endpoint in Endpoints)
            {
                bool ready = false;
                for (int attempt = 0; attempt < 150 && !ready; attempt++)
                {
                    if (processes.Any(process => process.HasExited)) throw new InvalidOperationException("Service exited before readiness. Inspect scenario diagnostics.");
                    try
                    {
                        using var response = await Http.GetAsync(endpoint.Value + "/health");
                        ready = response.IsSuccessStatusCode;
                    }
                    catch (HttpRequestException) { }
                    if (!ready) await Task.Delay(100);
                }
                if (!ready) throw new InvalidOperationException("Service readiness deadline elapsed.");
            }
            if (await Number("inventory", "/stock", "available") != 1 || await Number("payments", "/charges", "count") != 0)
                throw new InvalidOperationException("Scenario-owned stock and payment preconditions are invalid.");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public Task<HttpResponseMessage> Order(string id, bool declined) => Http.PostAsJsonAsync(Endpoints["orders"] + "/orders/" + id, new { declinePayment = declined });

    public async Task<string?> WaitForOrder(string id, string expected)
    {
        string? status = null;
        var elapsed = Stopwatch.StartNew();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (elapsed.Elapsed < TimeSpan.FromSeconds(10))
        {
            using var response = await Http.GetAsync(Endpoints["orders"] + "/orders/" + id, deadline.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                status = body.RootElement.GetProperty("status").GetString();
                if (status == expected) return status;
            }
            var remaining = TimeSpan.FromSeconds(10) - elapsed.Elapsed;
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining < TimeSpan.FromMilliseconds(100) ? remaining : TimeSpan.FromMilliseconds(100));
        }
        return status;
    }

    public async Task<long> Number(string service, string path, string property)
    {
        using var response = await Http.GetAsync(Endpoints[service] + path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty(property).GetInt64();
    }

    public async ValueTask DisposeAsync()
    {
        Exception? checkpointFailure = null;
        foreach (var listener in listeners) listener.Stop();
        listeners.Clear();
        foreach (var process in processes)
        {
            try { if (!process.HasExited) TadaRuntimeCheckpoint.Complete(process); }
            catch (Exception error) { checkpointFailure ??= error; }
            finally
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                process.Dispose();
            }
        }
        processes.Clear();
        await Task.WhenAll(diagnostics);
        Http.Dispose();
        if (Environment.GetEnvironmentVariable("TADA_RUN_DIRECTORY") == null && Directory.Exists(context.WorkingDirectory))
            Directory.Delete(context.WorkingDirectory, recursive: true);
        if (checkpointFailure != null) throw new InvalidOperationException("Service checkpoint failed.", checkpointFailure);
    }

    private static async Task Save(StreamReader reader, string path) => await File.WriteAllTextAsync(path, await reader.ReadToEndAsync());
}
