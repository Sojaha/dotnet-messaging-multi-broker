var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ──────────────────────────────────────────────────────────

var rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()          // exposes :15672 management UI
    .WithLifetime(ContainerLifetime.Persistent);

var seq = builder.AddSeq("seq")
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
