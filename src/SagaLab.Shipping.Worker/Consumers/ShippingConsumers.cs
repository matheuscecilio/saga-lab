using MassTransit;
using Microsoft.Extensions.Options;
using SagaLab.Contracts.Shipping;

namespace SagaLab.Shipping.Worker.Consumers;

public sealed class ShippingSimulationOptions
{
    public const string SectionName = "Simulation";

    public bool FailShipmentCreation { get; set; }
}

public sealed class CreateShipmentConsumer(IOptions<ShippingSimulationOptions> options) : IConsumer<CreateShipment>
{
    public Task Consume(ConsumeContext<CreateShipment> context)
    {
        if (options.Value.FailShipmentCreation)
        {
            return context.Publish(new ShipmentCreationFailed(
                context.Message.OrderId,
                "Shipment creation was rejected by the configured simulation.",
                DateTimeOffset.UtcNow));
        }

        return context.Publish(new ShipmentCreated(
            context.Message.OrderId,
            NewId.NextGuid(),
            DateTimeOffset.UtcNow));
    }
}

public sealed class CancelShipmentConsumer : IConsumer<CancelShipment>
{
    public Task Consume(ConsumeContext<CancelShipment> context) =>
        context.Publish(new ShipmentCancelled(
            context.Message.OrderId,
            context.Message.ShipmentId,
            DateTimeOffset.UtcNow));
}
