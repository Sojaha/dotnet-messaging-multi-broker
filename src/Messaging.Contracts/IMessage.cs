// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts;

/// <summary>
/// Base envelope carried by every message type.
/// Not used directly in consumer handlers — use IEvent, ICommand, or IQuery&lt;TResult&gt;.
/// </summary>
public interface IMessage
{
    /// <summary>Unique identifier for this message instance.</summary>
    Guid MessageId { get; }

    /// <summary>
    /// Correlation identifier propagated across all messages in the same
    /// logical operation. Sourced from Activity.TraceId when available.
    /// Carried in both the JSON payload and the x-correlation-id AMQP header.
    /// A consumer that publishes a downstream message MUST propagate this value,
    /// never generate a new one.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>UTC timestamp of when this message was created.</summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>Schema version of this contract. See ADR-0010.</summary>
    int SchemaVersion { get; }
}
