# ADR-0007: System.Text.Json for wire serialization

## Status

Accepted

## Context

Three consumer frameworks each have their own default serializer. NServiceBus defaults to its own XML-based serializer; MassTransit defaults to its JSON envelope format; RabbitMQ.Client has no built-in serializer. To share a single wire format all three must use the same serialization settings.

## Decision

Use `System.Text.Json` with camelCase property naming and null-value omission, configured in `MessagingJsonOptions.Default`. NServiceBus is configured with `SystemJsonSerializer`. MassTransit is configured with `UseRawJsonSerializer` / `UseRawJsonDeserializer`. The raw RmqClient consumer deserializes directly using `MessagingJsonOptions.Default`.

## Consequences

- All three consumers read identical bytes from the broker.
- No framework-specific envelope overhead in the message body.
- NServiceBus requires additional configuration (`IMessageTypeResolver`) to read `x-message-type` instead of its default `NServiceBus.EnclosedMessageTypes` header.
