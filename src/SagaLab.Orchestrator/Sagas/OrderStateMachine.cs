using MassTransit;
using SagaLab.Contracts.Inventory;
using SagaLab.Contracts.Orders;
using SagaLab.Contracts.Payments;
using SagaLab.Contracts.Shipping;

namespace SagaLab.Orchestrator.Sagas;

public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State ReservingInventory { get; private set; } = null!;

    public State AuthorizingPayment { get; private set; } = null!;

    public State CreatingShipment { get; private set; } = null!;

    public State RefundingPayment { get; private set; } = null!;

    public State ReleasingInventory { get; private set; } = null!;

    public State Completed { get; private set; } = null!;

    public State Cancelled { get; private set; } = null!;

    public Event<SubmitOrder> OrderSubmitted { get; private set; } = null!;

    public Event<InventoryReserved> InventoryReserved { get; private set; } = null!;

    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; } = null!;

    public Event<PaymentAuthorized> PaymentAuthorized { get; private set; } = null!;

    public Event<PaymentAuthorizationFailed> PaymentAuthorizationFailed { get; private set; } = null!;

    public Event<PaymentRefunded> PaymentRefunded { get; private set; } = null!;

    public Event<ShipmentCreated> ShipmentCreated { get; private set; } = null!;

    public Event<ShipmentCreationFailed> ShipmentCreationFailed { get; private set; } = null!;

    public Event<InventoryReleased> InventoryReleased { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(instance => instance.CurrentState);

        Event(() => OrderSubmitted, configuration =>
        {
            configuration.CorrelateById(context => context.Message.OrderId);
            configuration.SelectId(context => context.Message.OrderId);
        });
        Event(() => InventoryReserved, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentAuthorized, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentAuthorizationFailed, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefunded, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => ShipmentCreated, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => ShipmentCreationFailed, configuration => configuration.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReleased, configuration => configuration.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(context => InitializeOrder(context.Saga, context.Message))
                .Publish(context => new ReserveInventory(context.Message.OrderId, context.Message.Items))
                .TransitionTo(ReservingInventory));

        During(ReservingInventory,
            When(InventoryReserved)
                .Then(context => context.Saga.ReservationId = context.Message.ReservationId)
                .Publish(context => new AuthorizePayment(context.Saga.OrderId, context.Saga.TotalAmount, context.Saga.Currency))
                .TransitionTo(AuthorizingPayment),
            When(InventoryReservationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Publish(context => new OrderCancelled(context.Saga.OrderId, context.Message.Reason, DateTimeOffset.UtcNow))
                .TransitionTo(Cancelled));

        During(AuthorizingPayment,
            When(PaymentAuthorized)
                .Then(context => context.Saga.PaymentId = context.Message.PaymentId)
                .Publish(context => new CreateShipment(context.Saga.OrderId))
                .TransitionTo(CreatingShipment),
            When(PaymentAuthorizationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Publish(context => new ReleaseInventory(
                    context.Saga.OrderId,
                    context.Saga.ReservationId ?? throw new InvalidOperationException("ReservationId is required before compensation."),
                    context.Message.Reason))
                .TransitionTo(ReleasingInventory));

        During(CreatingShipment,
            When(ShipmentCreated)
                .Then(context => context.Saga.ShipmentId = context.Message.ShipmentId)
                .Publish(context => new OrderCompleted(
                    context.Saga.OrderId,
                    context.Saga.ReservationId ?? throw new InvalidOperationException("ReservationId is required for completion."),
                    context.Saga.PaymentId ?? throw new InvalidOperationException("PaymentId is required for completion."),
                    context.Message.ShipmentId,
                    DateTimeOffset.UtcNow))
                .TransitionTo(Completed),
            When(ShipmentCreationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Publish(context => new RefundPayment(
                    context.Saga.OrderId,
                    context.Saga.PaymentId ?? throw new InvalidOperationException("PaymentId is required before refund."),
                    context.Message.Reason))
                .TransitionTo(RefundingPayment));

        During(RefundingPayment,
            When(PaymentRefunded)
                .Publish(context => new ReleaseInventory(
                    context.Saga.OrderId,
                    context.Saga.ReservationId ?? throw new InvalidOperationException("ReservationId is required before release."),
                    context.Saga.FailureReason ?? "Shipment creation failed."))
                .TransitionTo(ReleasingInventory));

        During(ReleasingInventory,
            When(InventoryReleased)
                .Publish(context => new OrderCancelled(
                    context.Saga.OrderId,
                    context.Saga.FailureReason ?? "Order cancelled after compensation.",
                    DateTimeOffset.UtcNow))
                .TransitionTo(Cancelled));
    }

    private static void InitializeOrder(OrderState state, SubmitOrder message)
    {
        state.OrderId = message.OrderId;
        state.TotalAmount = message.TotalAmount;
        state.Currency = message.Currency;
        state.SubmittedAt = message.SubmittedAt;
    }
}
