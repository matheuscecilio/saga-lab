using SagaLab.Contracts.Orders;

namespace SagaLab.Contracts.Inventory;

public sealed record ReserveInventory(Guid OrderId, IReadOnlyCollection<OrderItem> Items);

public sealed record InventoryReserved(Guid OrderId, Guid ReservationId, DateTimeOffset ReservedAt);

public sealed record InventoryReservationFailed(Guid OrderId, string Reason, DateTimeOffset FailedAt);

public sealed record ReleaseInventory(Guid OrderId, Guid ReservationId, string Reason);

public sealed record InventoryReleased(Guid OrderId, Guid ReservationId, DateTimeOffset ReleasedAt);
