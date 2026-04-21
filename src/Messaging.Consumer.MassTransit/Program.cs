// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MassTransit;
using Messaging.Consumer.MassTransit;
using Messaging.Contracts.Topology;
using Messaging.ServiceDefaults;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));

        // Raw JSON serializer for publishing — stamps MT transport headers on outgoing messages
        cfg.UseRawJsonSerializer(RawSerializerOptions.AddTransportHeaders);

        cfg.ReceiveEndpoint(Queues.MassTransitOrderPlaced, ep =>
        {
            // AnyMessageType must be per-endpoint so the deserializer can inspect
            // the endpoint's registered consumers and map the message type correctly.
            // At cfg level it runs before consumers are wired, so routing fails silently.
            ep.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
            ep.Bind(Exchanges.OrderEvents, b =>
            {
                b.RoutingKey = RoutingKeys.OrderPlaced;
                b.ExchangeType = "topic";
            });
            ep.ConfigureConsumer<OrderPlacedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint(Queues.MassTransitOrderCancelled, ep =>
        {
            ep.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
            ep.Bind(Exchanges.OrderEvents, b =>
            {
                b.RoutingKey = RoutingKeys.OrderCancelled;
                b.ExchangeType = "topic";
            });
            ep.ConfigureConsumer<OrderCancelledConsumer>(ctx);
        });
    });
});

builder.Build().Run();
