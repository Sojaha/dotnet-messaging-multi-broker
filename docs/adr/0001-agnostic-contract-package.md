# ADR-0001: Broker-agnostic contract package

## Status

Accepted

## Context

Three different messaging frameworks (NServiceBus, MassTransit, RabbitMQ.Client) must share the same message types, routing keys, and exchange names without coupling any framework-specific dependency into the shared contract library. If contracts depended on a specific framework, consuming services would be forced to pull in that framework transitively.

## Decision

Create `Messaging.Contracts` as a zero-dependency class library containing all message interfaces, record types, topology constants, and versioning primitives. No `<PackageReference>` is allowed in this project. An MSBuild guard (`EnforceContractsPurity`) enforces this at build time.

## Consequences

- Any service can take a dependency on `Messaging.Contracts` without pulling in RabbitMQ, NServiceBus, or MassTransit.
- Adding a new message type is a single-project change.
- The contracts project cannot use any serialization, DI, or transport helpers directly.
