using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaLab.Orchestrator.Persistence;
using SagaLab.Orchestrator.Sagas;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var sagaConnectionString = builder.Configuration.GetConnectionString("SagaDatabase")
    ?? "Host=localhost;Port=5432;Database=sagalab;Username=sagalab;Password=sagalab";

builder.Services.AddDbContext<OrderSagaDbContext>(options => options.UseNpgsql(sagaConnectionString));

builder.Services.AddMassTransit(configuration =>
{
    configuration.SetKebabCaseEndpointNameFormatter();
    configuration.AddEntityFrameworkOutbox<OrderSagaDbContext>(outbox => outbox.UsePostgres());
    configuration.AddSagaStateMachine<OrderStateMachine, OrderState, OrderStateDefinition>()
        .EntityFrameworkRepository(repository =>
        {
            repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            repository.ExistingDbContext<OrderSagaDbContext>();
            repository.UsePostgres();
        });
    configuration.UsingRabbitMq((context, busConfiguration) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("RabbitMq")
            ?? "amqp://sagalab:sagalab@localhost:5672/";

        busConfiguration.Host(new Uri(connectionString));
        busConfiguration.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
