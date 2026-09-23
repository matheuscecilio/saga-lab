using MassTransit;
using SagaLab.Payment.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.Configure<PaymentSimulationOptions>(
    builder.Configuration.GetSection(PaymentSimulationOptions.SectionName));
builder.Services.AddMassTransit(configuration =>
{
    configuration.SetKebabCaseEndpointNameFormatter();
    configuration.AddConsumer<AuthorizePaymentConsumer>();
    configuration.AddConsumer<RefundPaymentConsumer>();
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
