namespace SagaLab.Order.Api.Contracts;

public sealed record CreateOrderRequest(
    Guid? OrderId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(string Sku, int Quantity, decimal UnitPrice);

public sealed record SubmitOrderResponse(Guid OrderId, string Status);
