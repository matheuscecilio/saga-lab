using MassTransit;
using SagaLab.Orchestrator.Persistence;

namespace SagaLab.Orchestrator.Sagas;

public sealed class OrderStateDefinition : SagaDefinition<OrderState>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<OrderState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(100, 500, 1000));
        endpointConfigurator.UseEntityFrameworkOutbox<OrderSagaDbContext>(context);
    }
}
