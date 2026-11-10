using System.Net.Http.Json;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
string role = builder.Configuration["role"] ?? throw new InvalidOperationException("A service role is required.");
string database = builder.Configuration["database"] ?? throw new InvalidOperationException("A scenario database is required.");
string inventory = builder.Configuration["inventory"] ?? "";
string payments = builder.Configuration["payments"] ?? "";
string orders = builder.Configuration["orders"] ?? "";
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
using (var connection = Open())
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS Stock (Id INTEGER PRIMARY KEY, Available INTEGER NOT NULL);
        INSERT OR IGNORE INTO Stock VALUES (1, 1);
        CREATE TABLE IF NOT EXISTS Reservations (Id TEXT PRIMARY KEY, Active INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS Payments (Id TEXT PRIMARY KEY, Status TEXT NOT NULL, Delivered INTEGER NOT NULL DEFAULT 0);
        CREATE TABLE IF NOT EXISTS Orders (Id TEXT PRIMARY KEY, Status TEXT NOT NULL, Decline INTEGER NOT NULL);
        """;
    command.ExecuteNonQuery();
}
app.MapGet("/health", () => Results.Ok(new { role }));
if (role == "inventory")
{
    app.MapGet("/stock", () => Results.Ok(new { available = Scalar("SELECT Available FROM Stock WHERE Id=1") }));
    app.MapPost("/reserve/{id}", (string id) =>
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$id", id);
        command.CommandText = "SELECT COUNT(*) FROM Reservations WHERE Id=$id";
        bool exists = (long)command.ExecuteScalar()! > 0;
        if (!exists)
        {
            command.CommandText = "UPDATE Stock SET Available=Available-1 WHERE Id=1 AND Available>0";
            if (command.ExecuteNonQuery() == 0) return Results.Conflict(new { error = "unavailable" });
            command.CommandText = "INSERT INTO Reservations VALUES ($id,1)";
            command.ExecuteNonQuery();
        }
        transaction.Commit();
        return Results.Ok(new { reservationId = id });
    });
    app.MapPost("/release/{id}", (string id) =>
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$id", id);
        command.CommandText = "UPDATE Reservations SET Active=0 WHERE Id=$id AND Active=1";
        if (command.ExecuteNonQuery() > 0)
        {
            command.CommandText = "UPDATE Stock SET Available=Available+1 WHERE Id=1";
            command.ExecuteNonQuery();
        }
        transaction.Commit();
        return Results.Ok();
    });
}
if (role == "payments")
{
    app.MapPost("/payments/{id}", (string id, PaymentRequest request) =>
    {
        Execute("INSERT OR IGNORE INTO Payments(Id,Status) VALUES($id,$status)", id, request.DeclinePayment ? "declined" : "approved");
        return Results.Accepted();
    });
    app.MapGet("/payments/{id}", (string id) =>
    {
        string? status = Status("Payments", id);
        return status == null ? Results.NotFound() : Results.Ok(new PaymentOutcome(1, id, status));
    });
    app.MapGet("/charges", () => Results.Ok(new { count = Scalar("SELECT COUNT(*) FROM Payments WHERE Status='approved'") }));
}
if (role == "orders")
{
    app.MapPost("/orders/{id}", (string id, PaymentRequest request) =>
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Orders VALUES($id,'pending',$decline)";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$decline", request.DeclinePayment ? 1 : 0);
        command.ExecuteNonQuery();
        return Results.Accepted();
    });
    app.MapGet("/orders/{id}", (string id) =>
    {
        string? status = Status("Orders", id);
        return status == null ? Results.NotFound() : Results.Ok(new { orderId = id, status });
    });
    app.MapPost("/payment-outcomes", async (PaymentOutcome outcome) =>
    {
        if (outcome.SchemaVersion != 1 || string.IsNullOrWhiteSpace(outcome.OrderId) || outcome.Status is not ("approved" or "declined"))
            return Results.BadRequest(new { error = "unsupported_payment_outcome" });
        string? status = Status("Orders", outcome.OrderId);
        if (status == null) return Results.NotFound();
        if (status is "completed" or "rejected") return Results.Ok();
        if (outcome.Status == "declined")
        {
            using var release = await client.PostAsync(inventory + "/release/" + Uri.EscapeDataString(outcome.OrderId), null);
            if (!release.IsSuccessStatusCode) return Results.StatusCode(503);
        }
        Execute("UPDATE Orders SET Status=$status WHERE Id=$id", outcome.OrderId, outcome.Status == "approved" ? "completed" : "rejected");
        return Results.Ok();
    });
}
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(Work));
await app.RunAsync();

SqliteConnection Open()
{
    var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString());
    connection.Open();
    return connection;
}
long Scalar(string sql)
{
    using var connection = Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return (long)command.ExecuteScalar()!;
}
string? Status(string table, string id)
{
    using var connection = Open();
    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT Status FROM {table} WHERE Id=$id";
    command.Parameters.AddWithValue("$id", id);
    return command.ExecuteScalar() as string;
}
void Execute(string sql, string id, string status)
{
    using var connection = Open();
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$status", status);
    command.ExecuteNonQuery();
}
async Task Work()
{
    var cancellation = app.Lifetime.ApplicationStopping;
    while (!cancellation.IsCancellationRequested)
    {
        try
        {
            var pending = new List<(string Id, string Status, bool Decline)>();
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = role == "orders" ? "SELECT Id,Status,Decline FROM Orders WHERE Status='pending'"
                    : "SELECT Id,Status,0 FROM Payments WHERE Delivered=0";
                if (role != "inventory")
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read()) pending.Add((reader.GetString(0), reader.GetString(1), reader.GetInt64(2) != 0));
                }
            }
            foreach (var item in pending)
            {
                if (role == "orders")
                {
                    using var reservation = await client.PostAsync(inventory + "/reserve/" + Uri.EscapeDataString(item.Id), null, cancellation);
                    if (!reservation.IsSuccessStatusCode) continue;
                    using var response = await client.PostAsJsonAsync(payments + "/payments/" + Uri.EscapeDataString(item.Id), new PaymentRequest(item.Decline), cancellation);
                    if (response.IsSuccessStatusCode)
                        Execute("UPDATE Orders SET Status=$status WHERE Id=$id AND Status='pending'", item.Id, "awaiting_payment");
                }
                else if (role == "payments")
                {
                    using var response = await client.PostAsJsonAsync(orders + "/payment-outcomes", new PaymentOutcome(1, item.Id, item.Status), cancellation);
                    if (response.IsSuccessStatusCode) Execute("UPDATE Payments SET Delivered=1 WHERE Id=$id AND Status=$status", item.Id, item.Status);
                }
            }
            await Task.Delay(50, cancellation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return; }
        catch (HttpRequestException error) { app.Logger.LogInformation("Pending delivery retained. {Error}", error.Message); }
        catch (TaskCanceledException error) { app.Logger.LogInformation("Delivery deadline elapsed. {Error}", error.Message); }
    }
}
public sealed record PaymentRequest(bool DeclinePayment);
public sealed record PaymentOutcome(int SchemaVersion, string OrderId, string Status);
