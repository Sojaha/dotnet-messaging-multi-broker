namespace Messaging.Consumer.MassTransit;

using global::MassTransit;
using Messaging.Contracts.Orders.Events;

public sealed class OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger)
    : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        logger.LogInformation("[MassTransit] OrderCancelled — OrderId={OrderId} Reason={Reason}",
            context.Message.OrderId, context.Message.Reason);
        return Task.CompletedTask;
    }
}
