using MassTransit;

namespace SagaLab.Orchestrator.Sagas;

public sealed class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public Guid? ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public Guid? ShipmentId { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
