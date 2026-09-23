using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SagaLab.Order.Api.Contracts;
using SagaLab.Contracts.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddHealthChecks();
builder.Services.AddMassTransit(configuration =>
{
    configuration.UsingRabbitMq((context, busConfiguration) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("RabbitMq")
            ?? "amqp://sagalab:sagalab@localhost:5672/";

        busConfiguration.Host(new Uri(connectionString));
        busConfiguration.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/alive", () => Results.Ok(new { status = "healthy", service = "order-api" }));

app.MapPost("/orders", async (
    CreateOrderRequest request,
    IPublishEndpoint publishEndpoint,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var orderId = request.OrderId is { } requestedOrderId && requestedOrderId != Guid.Empty
        ? requestedOrderId
        : Guid.NewGuid();

    var orderItems = request.Items
        .Select(item => new OrderItem(item.Sku.Trim(), item.Quantity, item.UnitPrice))
        .ToArray();
    var currency = request.Currency.Trim().ToUpperInvariant();

    await publishEndpoint.Publish(
        new SubmitOrder(orderId, orderItems, request.TotalAmount, currency, DateTimeOffset.UtcNow),
        cancellationToken);

    SagaLab.Order.Api.OrderApiLog.OrderSubmitted(logger, orderId, orderItems.Length, request.TotalAmount, currency);

    return Results.Accepted($"/orders/{orderId}", new SubmitOrderResponse(orderId, "submitted"));
})
.WithName("SubmitOrder")
.Produces<SubmitOrderResponse>(StatusCodes.Status202Accepted)
.ProducesValidationProblem(StatusCodes.Status400BadRequest);

static Dictionary<string, string[]> Validate(CreateOrderRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.TotalAmount <= 0)
    {
        errors[nameof(request.TotalAmount)] = ["TotalAmount must be greater than zero."];
    }

    if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
    {
        errors[nameof(request.Currency)] = ["Currency must be a three-letter ISO code."];
    }

    if (request.Items is null || request.Items.Count == 0)
    {
        errors[nameof(request.Items)] = ["At least one item is required."];
    }
    else if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.Sku) || item.Quantity <= 0 || item.UnitPrice < 0))
    {
        errors[nameof(request.Items)] = ["Each item requires SKU, positive quantity, and non-negative unit price."];
    }

    return errors;
}

app.Run();

public partial class Program;
