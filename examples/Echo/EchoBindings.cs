using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[assembly: DoNotParallelize]

[Binding]
public sealed class EchoBindings
{
    private TadaAcceptance? context;
    private Process? api;
    private HttpClient? client;
    private HttpResponseMessage? response;
    private JsonElement body;
    private bool bodyParsed;

    [Given("an isolated echo API")]
    public void Start()
    {
        context = TadaAcceptance.Create();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        string address = "http://127.0.0.1:" + port;
        string assembly = context.SutAssembly;
        string dotnet = context.DotnetHost;
        var start = new ProcessStartInfo(dotnet) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = context.WorkingDirectory };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(address);
        api = Process.Start(start) ?? throw new InvalidOperationException("API launch failed.");
        api.BeginOutputReadLine();
        api.BeginErrorReadLine();
        client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(3) };
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            if (api.HasExited) throw new InvalidOperationException("API exited during readiness.");
            try { using var ready = client.GetAsync("/").GetAwaiter().GetResult(); break; }
            catch (HttpRequestException) { }
            if (deadline.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("API readiness unavailable.");
            Thread.Sleep(50);
        }
    }

    [When("I send the message {string}")]
    public void Send(string message)
    {
        response = client!.PostAsJsonAsync("/echo", new { message }).GetAwaiter().GetResult();
        string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        try { body = JsonSerializer.Deserialize<JsonElement>(content); bodyParsed = true; }
        catch (JsonException) { bodyParsed = false; }
    }
    [Then("the echo status is {int}")]
    public void Status(int code) => Assert.AreEqual(code, (int)response!.StatusCode);
    [Then("the echoed message is {string}")]
    public void Message(string text) => Assert.AreEqual(text, Text("message"));
    [Then("the error is {string}")]
    public void Error(string text) => Assert.AreEqual(text, Text("error"));

    private string? Text(string property)
    {
        Assert.IsTrue(bodyParsed && body.ValueKind == JsonValueKind.Object, "API response must be a JSON object.");
        Assert.IsTrue(body.TryGetProperty(property, out var value), $"API response must contain {property}.");
        Assert.AreEqual(JsonValueKind.String, value.ValueKind);
        return value.GetString();
    }

    [AfterScenario]
    public void Cleanup()
    {
        response?.Dispose();
        client?.Dispose();
        if (api == null) return;
        try { TadaRuntimeCheckpoint.Complete(api); }
        finally
        {
            if (!api.HasExited) api.Kill(entireProcessTree: true);
            api.WaitForExit(5000);
            api.Dispose();
        }
    }
}
