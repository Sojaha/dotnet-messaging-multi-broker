using Messaging.Infrastructure;
using Messaging.Infrastructure.Publishing;
using Messaging.Publisher;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();         // Aspire ServiceDefaults (OTel, health checks)
builder.AddRabbitMQClient("rabbitmq"); // Aspire RabbitMQ integration

builder.Services.AddSingleton<MessagePublisher>();
builder.Services.AddHostedService<PublisherWorker>();

var host = builder.Build();
host.Run();
