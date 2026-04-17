using Messaging.Consumer.RmqClient;
using Messaging.Infrastructure.Publishing;
using Messaging.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Aspire.RabbitMQ.Client pins to RabbitMQ.Client < 7.0.0 which conflicts with the
// 7.x IChannel API. Register IConnection directly from the Aspire connection string.
builder.Services.AddSingleton<IConnection>(_ =>
{
    string uri = builder.Configuration.GetConnectionString("rabbitmq")
        ?? "amqp://guest:guest@localhost:5672/";
    ConnectionFactory factory = new() { Uri = new Uri(uri) };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// Scrutor: scan Infrastructure + RmqClient assemblies.
// Picks up QueryReplyPublisher and MessageDispatcher as singletons.
// IChannel is NOT in DI — it flows as a method parameter from the worker's channel.
builder.Services.Scan(scan => scan
    .FromAssembliesOf(typeof(QueryReplyPublisher), typeof(RmqConsumerWorker))
    .AddClasses(c => c.Where(t => !typeof(IHostedService).IsAssignableFrom(t)))
    .AsSelf()
    .WithSingletonLifetime());

builder.Services.AddHostedService<RmqConsumerWorker>();

IHost host = builder.Build();
host.Run();
