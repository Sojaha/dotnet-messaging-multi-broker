using Messaging.Consumer.NServiceBus;
using Messaging.ServiceDefaults;
using Microsoft.Extensions.Hosting;
using NServiceBus;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

EndpointConfiguration endpointConfig = new("messaging.consumer.nsb");

TransportExtensions<RabbitMQTransport> transport = endpointConfig.UseTransport<RabbitMQTransport>();
transport.ConnectionString(builder.Configuration.GetConnectionString("rabbitmq")!);
transport.UseConventionalRoutingTopology(QueueType.Quorum);

endpointConfig.UseSerialization<SystemJsonSerializer>();
endpointConfig.EnableInstallers();

builder.UseNServiceBus(endpointConfig);

builder.Build().Run();
