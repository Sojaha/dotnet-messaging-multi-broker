namespace Messaging.Consumer.RmqClient;

using Messaging.Contracts;
using Messaging.Contracts.Orders.Commands;
using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Orders.Queries;
using Messaging.Infrastructure.Publishing;
using RabbitMQ.Client;

public sealed class MessageDispatcher(
    QueryReplyPublisher replyPublisher,
    ILogger<MessageDispatcher> logger)
{
    public Task DispatchAsync(
        IMessage message,
        IReadOnlyBasicProperties props,
        IChannel channel,
        CancellationToken ct) => message switch
    {
        // Events — fire and forget, no reply
        OrderPlaced    m => HandleAsync(m, ct),
        OrderCancelled m => HandleAsync(m, ct),

        // Command — fire and forget, handler publishes downstream event
        CancelOrder    m => HandleAsync(m, ct),

        // Query — must send a reply
        GetOrderStatus m => HandleAsync(m, props, ct),

        _ => Task.CompletedTask,
    };

    // ── Event handlers ────────────────────────────────────────────────────────

    private Task HandleAsync(OrderPlaced m, CancellationToken _)
    {
        logger.LogInformation(
            "[RmqClient][Event] OrderPlaced — OrderId={OrderId} CorrelationId={CorrelationId}",
            m.OrderId, m.CorrelationId);
        return Task.CompletedTask;
    }

    private Task HandleAsync(OrderCancelled m, CancellationToken _)
    {
        logger.LogInformation(
            "[RmqClient][Event] OrderCancelled — OrderId={OrderId} Reason={Reason} CorrelationId={CorrelationId}",
            m.OrderId, m.Reason, m.CorrelationId);
        return Task.CompletedTask;
    }

    // ── Command handler ───────────────────────────────────────────────────────

    private Task HandleAsync(CancelOrder m, CancellationToken _)
    {
        logger.LogInformation(
            "[RmqClient][Command] CancelOrder — OrderId={OrderId} RequestedBy={RequestedBy} CorrelationId={CorrelationId}",
            m.OrderId, m.RequestedBy, m.CorrelationId);
        // In a real service, cancel the order here then publish an OrderCancelled event,
        // propagating m.CorrelationId to maintain the trace chain.
        return Task.CompletedTask;
    }

    // ── Query handler ─────────────────────────────────────────────────────────

    private async Task HandleAsync(GetOrderStatus m, IReadOnlyBasicProperties props, CancellationToken ct)
    {
        logger.LogInformation(
            "[RmqClient][Query] GetOrderStatus — OrderId={OrderId} CorrelationId={CorrelationId}",
            m.OrderId, m.CorrelationId);

        var result = new OrderStatusResult(
            OrderId:       m.OrderId,
            Status:        "Confirmed",    // stub — real implementation queries a DB
            CorrelationId: m.CorrelationId);

        if (props.ReplyTo is not null)
            await replyPublisher.ReplyAsync(result, props.ReplyTo, m.CorrelationId, ct);
        else
            logger.LogWarning("[RmqClient][Query] GetOrderStatus received without ReplyTo — no reply sent");
    }
}
