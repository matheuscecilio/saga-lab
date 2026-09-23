using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SagaLab.Contracts.Inventory;
using SagaLab.Contracts.Orders;
using SagaLab.Inventory.Worker.Consumers;
using Xunit;

namespace SagaLab.ArchitectureTests.Consumers;

public sealed class InventoryConsumerTests
{
    [Fact]
    public async Task ReserveInventory_WhenSimulationAllowsIt_ShouldPublishReservation()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .Configure<InventorySimulationOptions>(options => options.FailReservation = false)
            .AddMassTransitTestHarness(configuration => configuration.AddConsumer<ReserveInventoryConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(new ReserveInventory(
                Guid.NewGuid(),
                [new OrderItem("BOOK-SAGA", 1, 99.90m)]));

            // Assert
            Assert.True(await harness.Consumed.Any<ReserveInventory>());
            Assert.True(await harness.Published.Any<InventoryReserved>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ReserveInventory_WhenSimulationRejectsIt_ShouldPublishReservationFailure()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .Configure<InventorySimulationOptions>(options => options.FailReservation = true)
            .AddMassTransitTestHarness(configuration => configuration.AddConsumer<ReserveInventoryConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(new ReserveInventory(
                Guid.NewGuid(),
                [new OrderItem("BOOK-SAGA", 1, 99.90m)]));

            // Assert
            Assert.True(await harness.Consumed.Any<ReserveInventory>());
            Assert.True(await harness.Published.Any<InventoryReservationFailed>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
