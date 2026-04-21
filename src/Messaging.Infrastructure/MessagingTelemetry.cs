// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Messaging.Infrastructure;

/// <summary>
/// Central telemetry instruments for the messaging layer.
/// ActivitySources follow OTel AMQP semantic conventions (messaging.*).
/// The Meter exposes custom counters and a processing-duration histogram.
/// </summary>
public static class MessagingTelemetry
{
    /// <summary>ActivitySource for publish-side spans (producer).</summary>
    public static readonly ActivitySource Publishing = new("Messaging.Publishing", "1.0.0");

    /// <summary>ActivitySource for consume-side spans (consumer).</summary>
    public static readonly ActivitySource Consuming = new("Messaging.Consuming", "1.0.0");

    private static readonly Meter _meter = new("Messaging", "1.0.0");

    /// <summary>Incremented once per successful BasicPublish call.</summary>
    public static readonly Counter<long> MessagesPublished =
        _meter.CreateCounter<long>(
            "messaging.published", "messages",
            "Total number of messages published to RabbitMQ");

    /// <summary>Incremented once per successfully acked message.</summary>
    public static readonly Counter<long> MessagesConsumed =
        _meter.CreateCounter<long>(
            "messaging.consumed", "messages",
            "Total number of messages processed and acked");

    /// <summary>Incremented each time a message is nacked with requeue=true.</summary>
    public static readonly Counter<long> MessagesRetried =
        _meter.CreateCounter<long>(
            "messaging.retried", "messages",
            "Total number of messages requeued after a transient error");

    /// <summary>Incremented each time a message is nacked with requeue=false (dead-lettered).</summary>
    public static readonly Counter<long> MessagesDeadLettered =
        _meter.CreateCounter<long>(
            "messaging.dead_lettered", "messages",
            "Total number of messages sent to dead-letter after exhausting retries");

    /// <summary>Processing wall-clock time from first byte received to ack/nack, in milliseconds.</summary>
    public static readonly Histogram<double> ProcessingDuration =
        _meter.CreateHistogram<double>(
            "messaging.process.duration", "ms",
            "Wall-clock time spent processing a single message (receive → ack/nack)");
}
