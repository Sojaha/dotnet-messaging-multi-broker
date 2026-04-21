// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Aspire.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ──────────────────────────────────────────────────────────

IResourceBuilder<RabbitMQServerResource> rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()          // exposes :15672 management UI
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SeqResource> seq = builder.AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Services ────────────────────────────────────────────────────────────────

builder.AddProject<Projects.Messaging_Publisher>("publisher")
    .WithReference(rabbit)
    .WithReference(seq)
    .WaitFor(rabbit);

builder.AddProject<Projects.Messaging_Consumer_NServiceBus>("consumer-nsb")
    .WithReference(rabbit)
    .WithReference(seq)
    .WaitFor(rabbit);

builder.AddProject<Projects.Messaging_Consumer_MassTransit>("consumer-masstransit")
    .WithReference(rabbit)
    .WithReference(seq)
    .WaitFor(rabbit);

builder.AddProject<Projects.Messaging_Consumer_RmqClient>("consumer-rmqclient")
    .WithReference(rabbit)
    .WithReference(seq)
    .WaitFor(rabbit);

builder.Build().Run();
