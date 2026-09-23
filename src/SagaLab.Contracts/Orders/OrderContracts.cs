namespace SagaLab.Contracts.Orders;

public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

public sealed record SubmitOrder(
    Guid OrderId,
    IReadOnlyCollection<OrderItem> Items,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset SubmittedAt);

public sealed record OrderCompleted(
    Guid OrderId,
    Guid ReservationId,
    Guid PaymentId,
    Guid ShipmentId,
    DateTimeOffset CompletedAt);

public sealed record OrderCancelled(Guid OrderId, string Reason, DateTimeOffset CancelledAt);
