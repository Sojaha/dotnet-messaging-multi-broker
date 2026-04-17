# Messaging — broker-agnostic messaging on RabbitMQ

A .NET 10 monorepo demonstrating a **broker-agnostic messaging architecture** on top of RabbitMQ. A single contracts library owns all message types; three independent consumers receive them using NServiceBus, MassTransit, and the raw RabbitMQ.Client.

## Message flow

```mermaid
flowchart LR
    P[Publisher] -->|orders.placed.v1| EV[(orders.events\ntopic exchange)]
    P -->|orders.cancel.v1| EC[(orders.commands\ndirect exchange)]
    P -->|orders.status.v1| EQ[(orders.queries\ndirect exchange)]

    EV -->|binding| Q1[nsb.orders.placed]
    EV -->|binding| Q2[mt.orders.placed]
    EV -->|binding| Q3[rmq.orders.placed]

    Q1 --> C1[Consumer\nNServiceBus]
    Q2 --> C2[Consumer\nMassTransit]
    Q3 --> C3[Consumer\nRmqClient]

    EC --> QC[orders.commands.cancel]
    QC --> C3

    EQ --> QQ[orders.queries.status]
    QQ --> C3
    C3 -->|reply| P

    Q1 -->|nack × 3| DLX[(dlx\ndirect exchange)]
    Q2 -->|nack × 3| DLX
    Q3 -->|nack × 3| DLX
    DLX --> DLQ1[nsb.orders.placed.dlq]
    DLX --> DLQ2[mt.orders.placed.dlq]
    DLX --> DLQ3[rmq.orders.placed.dlq]
```

## Quick start

```bash
# Prerequisites: .NET 10 SDK, Docker, Aspire workload
dotnet workload install aspire

dotnet run --project aspire/Messaging.AppHost
```

The Aspire dashboard opens at `http://localhost:18888`. The RabbitMQ management UI is at `http://localhost:15672` (guest/guest).

## Solution layout

```
src/
  Messaging.Contracts/          # Zero-dep contracts — IEvent, ICommand, IQuery<T>
  Messaging.Infrastructure/     # Topology helpers, serialization, publisher
  Messaging.Publisher/          # Demo publisher Worker Service
  Messaging.Consumer.NServiceBus/
  Messaging.Consumer.MassTransit/
  Messaging.Consumer.RmqClient/
aspire/
  Messaging.AppHost/            # Aspire orchestration
  Messaging.ServiceDefaults/    # Shared OTel + Serilog defaults
tests/
  Messaging.Contracts.Tests/    # xUnit — topology, versioning, correlation
docs/adr/                       # Architecture Decision Records
```

## Architecture decisions

See [docs/adr/README.md](docs/adr/README.md) for the full ADR index.
