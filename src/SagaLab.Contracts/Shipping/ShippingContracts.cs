namespace SagaLab.Contracts.Shipping;

public sealed record CreateShipment(Guid OrderId);

public sealed record ShipmentCreated(Guid OrderId, Guid ShipmentId, DateTimeOffset CreatedAt);

public sealed record ShipmentCreationFailed(Guid OrderId, string Reason, DateTimeOffset FailedAt);

public sealed record CancelShipment(Guid OrderId, Guid ShipmentId, string Reason);

public sealed record ShipmentCancelled(Guid OrderId, Guid ShipmentId, DateTimeOffset CancelledAt);
