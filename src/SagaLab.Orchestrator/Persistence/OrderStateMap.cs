using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaLab.Orchestrator.Sagas;

namespace SagaLab.Orchestrator.Persistence;

public sealed class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("order_states");
        entity.Property(state => state.CurrentState).HasMaxLength(64);
        entity.Property(state => state.Currency).HasMaxLength(3);
        entity.Property(state => state.FailureReason).HasMaxLength(512);
    }
}
