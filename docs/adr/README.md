# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the Messaging solution, written in [MADR format](https://adr.github.io/madr/).

## Architecture overview

```mermaid
graph TD
    subgraph Aspire["Aspire AppHost (orchestration)"]
        RMQ[(RabbitMQ\nbroker)]
        SEQ[(Seq\nlog server)]
    end

    subgraph src["src/"]
        CONTRACTS[Messaging.Contracts\nzero deps]
        INFRA[Messaging.Infrastructure\nRabbitMQ.Client]
        PUB[Messaging.Publisher\nWorker Service]
        NSB[Messaging.Consumer.NServiceBus\nWorker Service]
        MT[Messaging.Consumer.MassTransit\nWorker Service]
        RMQC[Messaging.Consumer.RmqClient\nWorker Service]
    end

    INFRA --> CONTRACTS
    PUB --> INFRA
    NSB --> INFRA
    MT --> INFRA
    RMQC --> INFRA

    PUB -->|publishes| RMQ
    RMQ -->|delivers| NSB
    RMQ -->|delivers| MT
    RMQ -->|delivers| RMQC

    PUB --> SEQ
    NSB --> SEQ
    MT --> SEQ
    RMQC --> SEQ
```

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-0000](0000-template.md) | Template | — |
| [ADR-0001](0001-agnostic-contract-package.md) | Broker-agnostic contract package | Accepted |
| [ADR-0002](0002-rabbitmq-sole-broker.md) | RabbitMQ as sole broker | Accepted |
| [ADR-0003](0003-topic-exchange-header-type-routing.md) | Topic exchange + header type routing | Accepted |
| [ADR-0004](0004-central-package-management.md) | Central package management | Accepted |
| [ADR-0005](0005-quorum-queues-dead-letter.md) | Quorum queues + dead-letter exchange | Accepted |
| [ADR-0006](0006-aspire-local-orchestration.md) | Aspire for local orchestration | Accepted |
| [ADR-0007](0007-system-text-json-serialization.md) | System.Text.Json serialization | Accepted |
| [ADR-0008](0008-versioning-routing-key-suffix.md) | Versioning via routing key suffix | Accepted |
