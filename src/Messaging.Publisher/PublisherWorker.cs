namespace Messaging.Publisher;

using System.Diagnostics;
using Messaging.Contracts.Orders.Commands;
using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Orders.Queries;
using Messaging.Contracts.Topology;
using Messaging.Contracts.Versioning;
using Messaging.Infrastructure;
using Messaging.Infrastructure.Publishing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class PublisherWorker(
    MessagePublisher publisher,
    IConnection connection,
    ILogger<PublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        // Declare all exchanges (publisher only — queues are owned by consumers)
        await TopologyInitializer.DeclareAsync(channel, ct);

        while (!ct.IsCancellationRequested)
        {
            var orderId = Guid.NewGuid();

            // CorrelationId comes from the ambient Activity TraceId created by Aspire/OTel.
            // If no Activity exists (e.g. in a background job), fall back to a new Guid.
            // A consumer that publishes a downstream message MUST propagate this value.
            var correlationId = Activity.Current?.TraceId.ToString()
                                ?? Guid.NewGuid().ToString();

            // ── 1. Event — broadcast to all consumers ─────────────────────
            var placed = new OrderPlaced(
                MessageId:     Guid.NewGuid(),
                CorrelationId: correlationId,
                OccurredOn:    DateTimeOffset.UtcNow,
                SchemaVersion: ContractVersions.OrderPlaced,
                OrderId:       orderId,
                CustomerId:    Guid.NewGuid(),
                TotalAmount:   99.99m,
                Currency:      "EUR");

            await publisher.PublishAsync(placed, ct);
            logger.LogInformation("[Event] OrderPlaced published {OrderId}", orderId);

            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            // ── 2. Command — point-to-point to one service ────────────────
            var cancel = new CancelOrder(
                MessageId:     Guid.NewGuid(),
                CorrelationId: correlationId,  // same correlation — same logical operation
                OccurredOn:    DateTimeOffset.UtcNow,
                SchemaVersion: ContractVersions.CancelOrder,
                OrderId:       orderId,
                RequestedBy:   "demo-publisher",
                Reason:        "Demo cancellation");

            await publisher.PublishAsync(cancel, ct);
            logger.LogInformation("[Command] CancelOrder sent {OrderId}", orderId);

            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            // ── 3. Query — request/reply ───────────────────────────────────
            var replyQueueName = (await channel.QueueDeclareAsync(
                queue: string.Empty, durable: false, exclusive: true,
                autoDelete: true, cancellationToken: ct)).QueueName;

            var query = new GetOrderStatus(
                MessageId:     Guid.NewGuid(),
                CorrelationId: correlationId,
                OccurredOn:    DateTimeOffset.UtcNow,
                SchemaVersion: ContractVersions.GetOrderStatus,
                OrderId:       orderId);

            await publisher.PublishAsync(query, ct, replyTo: replyQueueName);
            logger.LogInformation("[Query] GetOrderStatus sent {OrderId}, reply queue: {ReplyQueue}",
                orderId, replyQueueName);

            var reply = await WaitForReplyAsync<OrderStatusResult>(channel, replyQueueName, ct);
            logger.LogInformation("[Query] OrderStatusResult received — Status={Status}", reply?.Status);

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private static async Task<TResult?> WaitForReplyAsync<TResult>(
        IChannel channel, string replyQueue, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<TResult?>();
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, ea) =>
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<TResult>(
                ea.Body.Span,
                Messaging.Infrastructure.Serialization.MessagingJsonOptions.Default);
            tcs.TrySetResult(result);
            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(replyQueue, autoAck: true, consumer, cancellationToken: ct);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked  = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        linked.Token.Register(() => tcs.TrySetResult(default));

        return await tcs.Task;
    }
}
