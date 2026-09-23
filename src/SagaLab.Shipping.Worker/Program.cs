using MassTransit;
using SagaLab.Shipping.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.Configure<ShippingSimulationOptions>(
    builder.Configuration.GetSection(ShippingSimulationOptions.SectionName));
builder.Services.AddMassTransit(configuration =>
{
    configuration.SetKebabCaseEndpointNameFormatter();
    configuration.AddConsumer<CreateShipmentConsumer>();
    configuration.AddConsumer<CancelShipmentConsumer>();
    configuration.AddConfigureEndpointsCallback((context, _, endpointConfiguration) =>
    {
        endpointConfiguration.UseMessageRetry(retry => retry.Intervals(100, 500, 1000));
        endpointConfiguration.UseInMemoryOutbox(context);
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
