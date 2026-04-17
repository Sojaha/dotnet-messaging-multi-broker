using Messaging.Consumer.RmqClient;
using Messaging.Infrastructure.Publishing;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient("rabbitmq");

builder.Services.AddSingleton<QueryReplyPublisher>();
builder.Services.AddSingleton<MessageDispatcher>();
builder.Services.AddHostedService<RmqConsumerWorker>();

builder.Build().Run();
