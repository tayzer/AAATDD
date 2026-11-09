using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
string database = Environment.GetEnvironmentVariable("TADA_DB_PATH") ?? throw new InvalidOperationException("TADA_DB_PATH is required.");
string connectionString = new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString();

app.MapGet("/products/{sku}/stock", (string sku) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    using var query = connection.CreateCommand();
    query.CommandText = "SELECT available FROM inventory WHERE product_sku = @sku";
    query.Parameters.AddWithValue("@sku", sku);
    object? stock = query.ExecuteScalar();
    return stock is null ? Results.NotFound() : Results.Ok(new { productSku = sku, available = Convert.ToInt64(stock) });
});

app.MapPost("/orders", (OrderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.CustomerId) ||
        string.IsNullOrWhiteSpace(request.ProductSku) || request.Quantity <= 0)
        return Results.BadRequest();
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    using var transaction = connection.BeginTransaction();
    using var reserve = connection.CreateCommand();
    reserve.Transaction = transaction;
    reserve.CommandText = "UPDATE inventory SET available = available - @quantity WHERE product_sku = @sku AND available >= @quantity";
    reserve.Parameters.AddWithValue("@quantity", request.Quantity);
    reserve.Parameters.AddWithValue("@sku", request.ProductSku);
    if (reserve.ExecuteNonQuery() != 1)
    {
        transaction.Rollback();
        return Results.Conflict(new { outcome = "rejected", reason = "insufficient_stock", requestId = request.RequestId });
    }
    string orderId = Guid.NewGuid().ToString("N");
    using var insert = connection.CreateCommand();
    insert.Transaction = transaction;
    insert.CommandText = "INSERT INTO orders(order_id, request_id, customer_id, product_sku, quantity) VALUES(@order, @request, @customer, @sku, @quantity)";
    insert.Parameters.AddWithValue("@order", orderId);
    insert.Parameters.AddWithValue("@request", request.RequestId);
    insert.Parameters.AddWithValue("@customer", request.CustomerId);
    insert.Parameters.AddWithValue("@sku", request.ProductSku);
    insert.Parameters.AddWithValue("@quantity", request.Quantity);
    insert.ExecuteNonQuery();
    transaction.Commit();
    return Results.Json(new { outcome = "accepted", requestId = request.RequestId, orderId }, statusCode: 201);
});
app.Run();
public sealed record OrderRequest(string RequestId, string CustomerId, string ProductSku, int Quantity);
