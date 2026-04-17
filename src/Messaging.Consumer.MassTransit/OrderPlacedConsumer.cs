namespace Messaging.Consumer.MassTransit;

using global::MassTransit;
using Messaging.Contracts.Orders.Events;

public sealed class OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        logger.LogInformation("[MassTransit] OrderPlaced — OrderId={OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}
