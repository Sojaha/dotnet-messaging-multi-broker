namespace Messaging.Consumer.RmqClient;

using System.Text;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Contracts.Topology;
using Messaging.Infrastructure;
using Messaging.Infrastructure.Serialization;
using Messaging.Infrastructure.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
            try
            {
                // RabbitMQ.Client delivers AMQP string headers as byte[] on the wire.
                // Cast via as string always returns null — use ReadHeaderString instead.
                string typeName = ReadHeaderString(ea.BasicProperties.Headers, "x-message-type")
                    ?? throw new InvalidOperationException("Missing x-message-type header");

                Type type = MessageTypeRegistry.Resolve(typeName);
                IMessage message = (IMessage)JsonSerializer.Deserialize(
                    ea.Body.Span, type, MessagingJsonOptions.Default)!;

                // Restore CorrelationId into ambient Activity for OTel span linking.
                // A valid W3C trace ID is exactly 32 lowercase hex characters;
                // a fallback Guid will be 36 chars with hyphens and is skipped.
                string correlationId = message.CorrelationId;
                using System.Diagnostics.Activity activity = new("messaging.consume");
                if (correlationId is { Length: 32 })
                {
                    activity.SetParentId(
                        System.Diagnostics.ActivityTraceId.CreateFromString(correlationId.AsSpan()),
                        System.Diagnostics.ActivitySpanId.CreateRandom(),
                        System.Diagnostics.ActivityTraceFlags.Recorded);
                }

                activity.Start();

                await dispatcher.DispatchAsync(message, ea.BasicProperties, channel, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex) when (retryCount < 3)
            {
                logger.LogWarning(ex, "Transient error on attempt {Attempt}, requeueing", retryCount + 1);
                SetRetryCount(ea.BasicProperties, retryCount + 1);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Poison message after {MaxAttempts} attempts, dead-lettering", 3);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
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
