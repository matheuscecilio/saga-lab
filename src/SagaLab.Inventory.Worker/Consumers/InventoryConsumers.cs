using MassTransit;
using Microsoft.Extensions.Options;
using SagaLab.Contracts.Inventory;

namespace SagaLab.Inventory.Worker.Consumers;

public sealed class InventorySimulationOptions
{
    public const string SectionName = "Simulation";

    public bool FailReservation { get; set; }
}

public sealed class ReserveInventoryConsumer(IOptions<InventorySimulationOptions> options) : IConsumer<ReserveInventory>
{
    public Task Consume(ConsumeContext<ReserveInventory> context)
    {
        if (options.Value.FailReservation)
        {
            return context.Publish(new InventoryReservationFailed(
                context.Message.OrderId,
                "Inventory reservation was rejected by the configured simulation.",
                DateTimeOffset.UtcNow));
        }

        return context.Publish(new InventoryReserved(
            context.Message.OrderId,
            NewId.NextGuid(),
            DateTimeOffset.UtcNow));
    }
}

public sealed class ReleaseInventoryConsumer : IConsumer<ReleaseInventory>
{
    public Task Consume(ConsumeContext<ReleaseInventory> context) =>
        context.Publish(new InventoryReleased(
            context.Message.OrderId,
            context.Message.ReservationId,
            DateTimeOffset.UtcNow));
}
