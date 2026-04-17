namespace Messaging.Consumer.NServiceBus;

using Messaging.Contracts.Orders.Events;
using NServiceBus;

public sealed class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        logger.LogInformation("[NServiceBus] OrderPlaced received — OrderId={OrderId} Amount={Amount} {Currency}",
            message.OrderId, message.TotalAmount, message.Currency);
        return Task.CompletedTask;
    }
}
