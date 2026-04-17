# ADR-0006: Aspire for local orchestration

## Status

Accepted

## Context

Running the full stack locally (RabbitMQ, Seq, publisher, three consumers) requires coordinating container startup, health checks, environment variable injection, and port binding. Shell scripts or Docker Compose would require per-developer maintenance.

## Decision

Use **.NET Aspire AppHost** (`Messaging.AppHost`) to declare all resources. Aspire manages container lifecycle, injects connection strings, and wires health-check dependencies via `.WaitFor(rabbit)`. `ContainerLifetime.Persistent` keeps RabbitMQ and Seq containers alive between runs.

## Consequences

- Single `dotnet run --project aspire/Messaging.AppHost` starts the entire environment.
- The Aspire dashboard provides live logs, traces, and resource health at `http://localhost:18888`.
- Requires the Aspire workload: `dotnet workload install aspire`.
