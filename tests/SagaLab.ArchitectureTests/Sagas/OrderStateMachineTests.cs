using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SagaLab.Contracts.Inventory;
using SagaLab.Contracts.Orders;
using SagaLab.Contracts.Payments;
using SagaLab.Contracts.Shipping;
using SagaLab.Orchestrator.Sagas;
using Xunit;

namespace SagaLab.ArchitectureTests.Sagas;

public sealed class OrderStateMachineTests
{
    [Fact]
    public async Task SubmitOrder_WhenAllOperationsSucceed_ShouldPublishOrderCompleted()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(CreateSubmitOrder(orderId));
            await harness.Bus.Publish(new InventoryReserved(orderId, reservationId, DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new PaymentAuthorized(orderId, paymentId, DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new ShipmentCreated(orderId, shipmentId, DateTimeOffset.UtcNow));

            // Assert
            Assert.True(await harness.Published.Any<ReserveInventory>());
            Assert.True(await harness.Published.Any<AuthorizePayment>());
            Assert.True(await harness.Published.Any<CreateShipment>());
            Assert.True(await harness.Published.Any<OrderCompleted>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ShipmentCreationFailed_WhenPaymentWasAuthorized_ShouldRefundAndReleaseInventory()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(CreateSubmitOrder(orderId));
            await harness.Bus.Publish(new InventoryReserved(orderId, reservationId, DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new PaymentAuthorized(orderId, paymentId, DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new ShipmentCreationFailed(orderId, "Carrier unavailable.", DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new PaymentRefunded(orderId, paymentId, DateTimeOffset.UtcNow));
            await harness.Bus.Publish(new InventoryReleased(orderId, reservationId, DateTimeOffset.UtcNow));

            // Assert
            Assert.True(await harness.Published.Any<RefundPayment>());
            Assert.True(await harness.Published.Any<ReleaseInventory>());
            Assert.True(await harness.Published.Any<OrderCancelled>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider CreateServiceProvider() => new ServiceCollection()
        .AddMassTransitTestHarness(configuration =>
        {
            configuration.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .InMemoryRepository();
        })
        .BuildServiceProvider(true);

    private static SubmitOrder CreateSubmitOrder(Guid orderId) => new(
        orderId,
        [new OrderItem("BOOK-SAGA", 1, 99.90m)],
        99.90m,
        "BRL",
        DateTimeOffset.UtcNow);
}
