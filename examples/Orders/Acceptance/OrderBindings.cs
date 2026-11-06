using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[assembly: DoNotParallelize]

namespace Orders.Acceptance;

[Binding]
public sealed class OrderBindings
{
    private readonly string requestId = Guid.NewGuid().ToString("N");
    private readonly string customerId = Guid.NewGuid().ToString("N");
    private string database = "";
    private Process? api;
    private HttpClient? http;
    private HttpResponseMessage? response;
    private JsonElement body;
    private bool bodyParsed;
    private bool seeded;

    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    [Given("product {string} has {int} units available")]
    public void Product(string sku, int stock)
    {
        string run = Environment.GetEnvironmentVariable("TADA_RUN_DIRECTORY") ?? throw new InvalidOperationException("Run directory required.");
        database = Path.Combine(run, "orders-" + Guid.NewGuid().ToString("N") + ".sqlite");
        using (var connection = Open(database, SqliteOpenMode.ReadWriteCreate))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE inventory(product_sku TEXT PRIMARY KEY NOT NULL, available INTEGER NOT NULL); CREATE TABLE orders(order_id TEXT PRIMARY KEY NOT NULL, request_id TEXT NOT NULL, customer_id TEXT NOT NULL, product_sku TEXT NOT NULL, quantity INTEGER NOT NULL); INSERT INTO inventory VALUES(@sku,@stock)";
            command.Parameters.AddWithValue("@sku", sku);
            command.Parameters.AddWithValue("@stock", stock);
            command.ExecuteNonQuery();
        }
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        string address = "http://127.0.0.1:" + port;
        string assembly = Environment.GetEnvironmentVariable("TADA_SUT_ASSEMBLY") ?? throw new InvalidOperationException("SUT assembly required.");
        string dotnet = Environment.GetEnvironmentVariable("TADA_DOTNET_HOST") ?? Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var start = new ProcessStartInfo(dotnet) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(address);
        start.Environment["TADA_DB_PATH"] = database;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        api = Process.Start(start) ?? throw new InvalidOperationException("Could not start API.");
        api.BeginOutputReadLine();
        api.BeginErrorReadLine();
        http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(3) };
        // Only startup readiness polls. Business submissions and post-response observations never retry.
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            if (api.HasExited) throw new InvalidOperationException("API exited during readiness.");
            try
            {
                using var ready = http.GetAsync($"/products/{sku}/stock").GetAwaiter().GetResult();
                if (ready.IsSuccessStatusCode) break;
            }
            catch (HttpRequestException) { }
            if (deadline.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("API startup unavailable.");
            Thread.Sleep(50);
        }
        using (var observer = Open(database, SqliteOpenMode.ReadOnly))
        {
            using var query = observer.CreateCommand();
            query.CommandText = "SELECT available FROM inventory WHERE product_sku=@sku";
            query.Parameters.AddWithValue("@sku", sku);
            if (Convert.ToInt64(query.ExecuteScalar()) != stock) throw new InvalidOperationException("Seed readiness mismatch.");
        }
        var publicStock = Stock(sku);
        if (publicStock != stock) throw new InvalidOperationException("Public seed readiness mismatch.");
        seeded = true;
    }

    [Given("the customer has no existing orders")]
    public void NoExistingOrders()
    {
        using var observer = Open(database, SqliteOpenMode.ReadOnly);
        using var query = observer.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM orders";
        if (Convert.ToInt64(query.ExecuteScalar()) != 0) throw new InvalidOperationException("Unexpected seeded orders.");
    }

    [When("the customer orders {int} units of {string}")]
    public void Submit(int quantity, string sku)
    {
        if (!seeded) throw new InvalidOperationException("Preconditions were not established.");
        response = http!.PostAsJsonAsync("/orders", new { requestId, customerId, productSku = sku, quantity }).GetAwaiter().GetResult();
        string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        try { body = JsonSerializer.Deserialize<JsonElement>(content); bodyParsed = true; }
        catch (JsonException) { bodyParsed = false; }
    }

    private string? Text(string property)
    {
        Assert.IsTrue(bodyParsed && body.ValueKind == JsonValueKind.Object, "API response must be a JSON object.");
        Assert.IsTrue(body.TryGetProperty(property, out var value), $"API response must contain {property}.");
        Assert.AreEqual(JsonValueKind.String, value.ValueKind, $"API response {property} must be a string.");
        return value.GetString();
    }

    [Then("the order is rejected")]
    public void Rejected()
    {
        Assert.AreEqual(HttpStatusCode.Conflict, response!.StatusCode);
        Assert.AreEqual("rejected", Text("outcome"));
        Assert.AreEqual("insufficient_stock", Text("reason"));
        Assert.AreEqual(requestId, Text("requestId"));
    }

    [Then("the order is accepted")]
    public void Accepted()
    {
        Assert.AreEqual(HttpStatusCode.Created, response!.StatusCode);
        Assert.AreEqual("accepted", Text("outcome"));
        Assert.AreEqual(requestId, Text("requestId"));
        Assert.IsFalse(string.IsNullOrEmpty(Text("orderId")));
    }

    [Then("no order is created for that request")]
    public void NoOrder()
    {
        using var observer = Open(database, SqliteOpenMode.ReadOnly);
        using var query = observer.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM orders";
        Assert.AreEqual(0L, Convert.ToInt64(query.ExecuteScalar()));
    }

    [Then("exactly one order is created for {int} units of {string}")]
    public void OneOrder(int quantity, string sku)
    {
        using var observer = Open(database, SqliteOpenMode.ReadOnly);
        using var query = observer.CreateCommand();
        query.CommandText = "SELECT order_id,request_id,customer_id,product_sku,quantity FROM orders";
        using var rows = query.ExecuteReader();
        Assert.IsTrue(rows.Read(), "Accepted order must exist.");
        Assert.AreEqual(Text("orderId"), rows.GetString(0));
        Assert.AreEqual(requestId, rows.GetString(1));
        Assert.AreEqual(customerId, rows.GetString(2));
        Assert.AreEqual(sku, rows.GetString(3));
        Assert.AreEqual(quantity, rows.GetInt32(4));
        Assert.IsFalse(rows.Read(), "Unexpected additional order.");
    }

    private int Stock(string sku)
    {
        using var result = http!.GetAsync($"/products/{sku}/stock").GetAwaiter().GetResult();
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var stock = result.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        Assert.AreEqual(sku, stock.GetProperty("productSku").GetString());
        return stock.GetProperty("available").GetInt32();
    }

    [Then("product {string} still has {int} units available")]
    [Then("product {string} has {int} units available")]
    public void StockIs(string sku, int stock) => Assert.AreEqual(stock, Stock(sku));

    [AfterScenario]
    public void Cleanup()
    {
        try
        {
            response?.Dispose();
            http?.Dispose();
            if (api != null) TadaRuntimeCheckpoint.Complete(api);
        }
        finally
        {
            try
            {
                if (api != null)
                {
                    if (!api.HasExited) api.Kill(entireProcessTree: true);
                    api.WaitForExit(5000);
                    api.Dispose();
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(database))
                {
                    File.Delete(database);
                    if (File.Exists(database)) throw new IOException("Required owned database cleanup failed.");
                }
            }
        }
    }
}
