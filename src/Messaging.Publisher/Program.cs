// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Infrastructure.Publishing;
using Messaging.Publisher;
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

// Scrutor: scan Infrastructure + Publisher assemblies.
// Registers all concrete non-hosted-service classes as singletons (AsSelf).
// MessagePublisher is picked up here — it takes IConnection (already registered above).
builder.Services.Scan(scan => scan
    .FromAssembliesOf(typeof(MessagePublisher), typeof(PublisherWorker))
    .AddClasses(c => c.Where(t => !typeof(IHostedService).IsAssignableFrom(t)))
    .AsSelf()
    .WithSingletonLifetime());

builder.Services.AddHostedService<PublisherWorker>();

IHost host = builder.Build();
host.Run();
