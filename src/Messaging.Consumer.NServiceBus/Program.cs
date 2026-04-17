using Messaging.Consumer.NServiceBus;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.UseNServiceBus(endpointConfig =>
{
    endpointConfig.EndpointName("messaging.consumer.nsb");

    var transport = endpointConfig.UseTransport<RabbitMQTransport>();
    transport.ConnectionString(builder.Configuration.GetConnectionString("rabbitmq")!);
    transport.UseConventionalRoutingTopology(QueueType.Quorum);

    endpointConfig.UseSerialization<SystemJsonSerializer>();
    endpointConfig.EnableInstallers();
});

builder.Build().Run();
