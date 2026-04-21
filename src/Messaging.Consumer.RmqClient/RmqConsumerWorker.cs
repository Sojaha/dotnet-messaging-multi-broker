// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Contracts.Topology;
using Messaging.Infrastructure;
using Messaging.Infrastructure.Serialization;
using Messaging.Infrastructure.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging.Consumer.RmqClient;

public sealed class RmqConsumerWorker(
    IConnection connection,
    MessageDispatcher dispatcher,
    ILogger<RmqConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        IChannel channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await TopologyInitializer.DeclareAsync(channel, ct);

        // Events — one queue per event type for this consumer
        await TopologyInitializer.DeclareQueueAsync(channel,
            Queues.RmqClientOrderPlaced, Exchanges.OrderEvents, RoutingKeys.OrderPlaced, ct);
        await TopologyInitializer.DeclareQueueAsync(channel,
            Queues.RmqClientOrderCancelled, Exchanges.OrderEvents, RoutingKeys.OrderCancelled, ct);

        // Command — shared queue (only one service binds to this)
        await TopologyInitializer.DeclareQueueAsync(channel,
            Queues.CancelOrder, Exchanges.OrderCommands, RoutingKeys.CancelOrder, ct);

        // Query — shared queue
        await TopologyInitializer.DeclareQueueAsync(channel,
            Queues.GetOrderStatus, Exchanges.OrderQueries, RoutingKeys.GetOrderStatus, ct);

        await channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: ct);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            int retryCount = GetRetryCount(ea.BasicProperties);
            string? typeName = null;

            // Restore the publisher's span as parent so consume spans are linked
            // to the publish trace in Aspire dashboard / Jaeger.
            // The publisher injects a W3C traceparent header; fall back to no parent
            // if the header is absent (e.g. messages from third-party publishers).
            ActivityContext parentContext = ExtractParentContext(ea.BasicProperties.Headers);
            using Activity? activity = MessagingTelemetry.Consuming.StartActivity(
                "messaging.consume", ActivityKind.Consumer, parentContext);

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                // RabbitMQ.Client delivers AMQP string headers as byte[] on the wire.
                // Cast via as string always returns null — use ReadHeaderString instead.
                typeName = ReadHeaderString(ea.BasicProperties.Headers, "x-message-type")
                    ?? throw new InvalidOperationException("Missing x-message-type header");

                Type type = MessageTypeRegistry.Resolve(typeName);
                IMessage message = (IMessage)JsonSerializer.Deserialize(
                    ea.Body.Span, type, MessagingJsonOptions.Default)!;

                activity?.SetTag("messaging.system",        "rabbitmq")
                         .SetTag("messaging.operation",     "receive")
                         .SetTag("messaging.destination",    ea.Exchange)
                         .SetTag("messaging.routing_key",    ea.RoutingKey)
                         .SetTag("messaging.message.type",   typeName)
                         .SetTag("messaging.message.id",     message.MessageId.ToString())
                         .SetTag("messaging.correlation_id", message.CorrelationId);

                await dispatcher.DispatchAsync(message, ea.BasicProperties, channel, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);

                MessagingTelemetry.MessagesConsumed.Add(1,
                    new KeyValuePair<string, object?>("messaging.message.type", typeName));
            }
            catch (Exception ex) when (retryCount < 3)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogWarning(ex, "Transient error on attempt {Attempt}, requeueing", retryCount + 1);
                SetRetryCount(ea.BasicProperties, retryCount + 1);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);

                MessagingTelemetry.MessagesRetried.Add(1,
                    new KeyValuePair<string, object?>("messaging.message.type", typeName ?? "unknown"));
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "Poison message after {MaxAttempts} attempts, dead-lettering", 3);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);

                MessagingTelemetry.MessagesDeadLettered.Add(1,
                    new KeyValuePair<string, object?>("messaging.message.type", typeName ?? "unknown"));
            }
            finally
            {
                sw.Stop();
                MessagingTelemetry.ProcessingDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("messaging.message.type", typeName ?? "unknown"));
            }
        };

        string[] queues =
        [
            Queues.RmqClientOrderPlaced,
            Queues.RmqClientOrderCancelled,
            Queues.CancelOrder,
            Queues.GetOrderStatus,
        ];

        foreach (string queue in queues)
            await channel.BasicConsumeAsync(queue, autoAck: false, consumer, cancellationToken: ct);

        await Task.Delay(Timeout.Infinite, ct);
    }

    /// <summary>
    /// Parses the W3C traceparent AMQP header injected by MessagePublisher and returns
    /// an ActivityContext so the consumer span is a child of the producer span.
    /// Format: 00-{32hex traceId}-{16hex spanId}-{2hex flags}
    /// Returns default (no parent) if the header is absent or malformed.
    /// </summary>
    private static ActivityContext ExtractParentContext(IDictionary<string, object?>? headers)
    {
        string? traceparent = ReadHeaderString(headers, "traceparent");
        if (traceparent is null)
            return default;

        // Expected length: 2 + 1 + 32 + 1 + 16 + 1 + 2 = 55
        ReadOnlySpan<char> span = traceparent.AsSpan();
        if (span.Length < 55 || span[2] != '-' || span[35] != '-' || span[52] != '-')
            return default;

        try
        {
            ActivityTraceId traceId = ActivityTraceId.CreateFromString(span.Slice(3, 32));
            ActivitySpanId  spanId  = ActivitySpanId.CreateFromString(span.Slice(36, 16));

            byte.TryParse(span.Slice(53, 2), NumberStyles.HexNumber, null, out byte flagsByte);
            ActivityTraceFlags flags = (ActivityTraceFlags)flagsByte;

            return new ActivityContext(traceId, spanId, flags, isRemote: true);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Reads a string header from an AMQP header dictionary.
    /// RabbitMQ.Client 7.x delivers string-valued headers as UTF-8 byte arrays;
    /// a direct cast to string always returns null.
    /// </summary>
    private static string? ReadHeaderString(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out object? value))
            return null;

        return value switch
        {
            string s => s,
            byte[] b => Encoding.UTF8.GetString(b),
            _        => null,
        };
    }

    private static int GetRetryCount(IReadOnlyBasicProperties props)
        => props.Headers?.TryGetValue("x-retry-count", out object? v) == true && v is int i ? i : 0;

    private static void SetRetryCount(IReadOnlyBasicProperties props, int count)
    {
        if (props is BasicProperties mutable)
            (mutable.Headers ??= new Dictionary<string, object?>())["x-retry-count"] = count;
    }
}
