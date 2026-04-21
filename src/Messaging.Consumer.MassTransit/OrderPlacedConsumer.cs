// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::MassTransit;
using Messaging.Contracts.Orders.Events;

namespace Messaging.Consumer.MassTransit;

public sealed class OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        logger.LogInformation("[MassTransit] OrderPlaced — OrderId={OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}
