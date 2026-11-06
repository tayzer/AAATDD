var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapPost("/echo", (EchoRequest request) => string.IsNullOrWhiteSpace(request.Message)
    ? Results.BadRequest(new { error = "message_required" })
    : Results.Ok(new { message = request.Message }));
app.Run();
public sealed record EchoRequest(string Message);
