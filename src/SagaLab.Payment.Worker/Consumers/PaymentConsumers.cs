using MassTransit;
using Microsoft.Extensions.Options;
using SagaLab.Contracts.Payments;

namespace SagaLab.Payment.Worker.Consumers;

public sealed class PaymentSimulationOptions
{
    public const string SectionName = "Simulation";

    public bool FailAuthorization { get; set; }
}

public sealed class AuthorizePaymentConsumer(IOptions<PaymentSimulationOptions> options) : IConsumer<AuthorizePayment>
{
    public Task Consume(ConsumeContext<AuthorizePayment> context)
    {
        if (options.Value.FailAuthorization)
        {
            return context.Publish(new PaymentAuthorizationFailed(
                context.Message.OrderId,
                "Payment authorization was rejected by the configured simulation.",
                DateTimeOffset.UtcNow));
        }

        return context.Publish(new PaymentAuthorized(
            context.Message.OrderId,
            NewId.NextGuid(),
            DateTimeOffset.UtcNow));
    }
}

public sealed class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    public Task Consume(ConsumeContext<RefundPayment> context) =>
        context.Publish(new PaymentRefunded(
            context.Message.OrderId,
            context.Message.PaymentId,
            DateTimeOffset.UtcNow));
}
