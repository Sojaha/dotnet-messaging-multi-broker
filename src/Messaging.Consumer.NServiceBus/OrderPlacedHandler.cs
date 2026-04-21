// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Orders.Events;
using NServiceBus;

namespace Messaging.Consumer.NServiceBus;

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
