# ADR-0002: RabbitMQ as sole broker

## Status

Accepted

## Context

The solution demonstrates broker-agnostic contracts but needs a concrete broker for local development and integration testing. Evaluating Azure Service Bus, Kafka, and RabbitMQ.

## Decision

Use RabbitMQ exclusively. It supports topic exchanges (fan-out semantics for events), direct exchanges (point-to-point for commands and queries), quorum queues, and dead-letter exchanges — all required by the topology design. It runs locally via Docker without cloud credentials.

## Consequences

- Local development requires Docker (or Podman).
- The Aspire AppHost manages the RabbitMQ container lifecycle.
- Switching to a different broker would require changes in `Messaging.Infrastructure` and consumer `Program.cs` files, but not in `Messaging.Contracts`.
