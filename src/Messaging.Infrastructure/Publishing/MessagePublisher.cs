// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Infrastructure.Serialization;
using Messaging.Infrastructure.Topology;
using RabbitMQ.Client;

namespace Messaging.Infrastructure.Publishing;

/// <summary>
/// Publishes any IMessage to its RabbitMQ exchange.
/// Owns a single long-lived IChannel created lazily on first publish.
/// </summary>
public sealed class MessagePublisher : IAsyncDisposable
{
    // Lazy<Task<T>> is safe for concurrent callers: the factory runs exactly once
    // and all waiters share the same Task. No CancellationToken needed here because
    // channel creation is done once at startup — if the connection is not yet ready,
    // the publish will surface the error naturally.
    private readonly Lazy<Task<IChannel>> _channel;

    public MessagePublisher(IConnection connection)
    {
        this._channel = new Lazy<Task<IChannel>>(
            () => connection.CreateChannelAsync());
    }

    /// <param name="replyTo">
    /// For IQuery messages only: the name of the caller's exclusive reply queue.
    /// Leave null for IEvent and ICommand.
    /// </param>
    public async Task PublishAsync<T>(
        T message,
        CancellationToken ct = default,
        string? replyTo = null)
        where T : IMessage
    {
        IChannel channel = await this._channel.Value;

        (string exchange, string routingKey) = TopologyResolver.Resolve<T>();
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJsonOptions.Default);

        // OTel producer span — named per AMQP semantic convention: "{exchange} publish"
        using Activity? activity = MessagingTelemetry.Publishing.StartActivity(
            $"{exchange} publish", ActivityKind.Producer);

        activity?.SetTag("messaging.system",              "rabbitmq")
                 .SetTag("messaging.operation",           "publish")
                 .SetTag("messaging.destination",          exchange)
                 .SetTag("messaging.destination.kind",    "exchange")
                 .SetTag("messaging.rabbitmq.routing_key", routingKey)
                 .SetTag("messaging.message.id",           message.MessageId.ToString())
                 .SetTag("messaging.message.type",         typeof(T).Name);

        // CorrelationId is set by the caller on the message record.
        // It comes from Activity.Current.TraceId — see PublisherWorker for the pattern.
        // Mirror it into both the AMQP native CorrelationId property and the
        // x-correlation-id header so all consumer frameworks can access it.
        //
        // Additionally inject the W3C traceparent header so the consumer can restore
        // the full parent span (trace ID + span ID) for proper distributed tracing.
        Dictionary<string, object?> headers = new()
        {
            ["x-message-type"]   = typeof(T).FullName,
            ["x-schema-version"] = (object)message.SchemaVersion,
            ["x-correlation-id"] = message.CorrelationId,
            ["x-retry-count"]    = (object)0,
        };

        if (activity is not null)
            headers["traceparent"] = FormatTraceparent(activity);

        BasicProperties props = new()
        {
            ContentType   = "application/json",
            DeliveryMode  = DeliveryModes.Persistent,
            MessageId     = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId,
            ReplyTo       = replyTo,
            Timestamp     = new AmqpTimestamp(message.OccurredOn.ToUnixTimeSeconds()),
            Headers       = headers,
        };

        try
        {
            await channel.BasicPublishAsync(exchange, routingKey, false, props, body, ct);

            MessagingTelemetry.MessagesPublished.Add(1,
                new KeyValuePair<string, object?>("messaging.destination",   exchange),
                new KeyValuePair<string, object?>("messaging.message.type",  typeof(T).Name));
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this._channel.IsValueCreated)
            await (await this._channel.Value).DisposeAsync();
    }

    // W3C traceparent: 00-{32hex traceId}-{16hex spanId}-{2hex flags}
    private static string FormatTraceparent(Activity activity)
    {
        string flags = activity.Recorded ? "01" : "00";
        return $"00-{activity.TraceId}-{activity.SpanId}-{flags}";
    }
}
