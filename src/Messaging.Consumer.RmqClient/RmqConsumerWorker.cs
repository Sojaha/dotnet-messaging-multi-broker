namespace Messaging.Consumer.RmqClient;

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
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

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

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var retryCount = GetRetryCount(ea.BasicProperties);
            try
            {
                var typeName = ea.BasicProperties.Headers?["x-message-type"] as string
                    ?? throw new InvalidOperationException("Missing x-message-type header");

                var type    = MessageTypeRegistry.Resolve(typeName);
                var message = (IMessage)JsonSerializer.Deserialize(
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

        var queues = new[]
        {
            Queues.RmqClientOrderPlaced,
            Queues.RmqClientOrderCancelled,
            Queues.CancelOrder,
            Queues.GetOrderStatus,
        };

        foreach (var queue in queues)
            await channel.BasicConsumeAsync(queue, autoAck: false, consumer, cancellationToken: ct);

        await Task.Delay(Timeout.Infinite, ct);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties props)
        => props.Headers?.TryGetValue("x-retry-count", out var v) == true && v is int i ? i : 0;

    private static void SetRetryCount(IReadOnlyBasicProperties props, int count)
    {
        if (props is BasicProperties mutable)
            (mutable.Headers ??= new Dictionary<string, object?>())["x-retry-count"] = count;
    }
}
