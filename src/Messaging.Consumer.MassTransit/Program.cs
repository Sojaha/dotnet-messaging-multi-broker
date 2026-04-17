using MassTransit;
using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Topology;
using Messaging.Consumer.MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));

        // Raw JSON mode: use the shared wire format, no MassTransit envelope
        cfg.UseRawJsonSerializer(RawSerializerOptions.AddTransportHeaders);
        cfg.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);

        cfg.ReceiveEndpoint(Queues.MassTransitOrderPlaced, ep =>
        {
            ep.Bind(Exchanges.OrderEvents, b =>
            {
                b.RoutingKey   = RoutingKeys.OrderPlaced;
                b.ExchangeType = "topic";
            });
            ep.ConfigureDeadLetterQueueErrorTransport();
            ep.ConfigureConsumer<OrderPlacedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint(Queues.MassTransitOrderCancelled, ep =>
        {
            ep.Bind(Exchanges.OrderEvents, b =>
            {
                b.RoutingKey   = RoutingKeys.OrderCancelled;
                b.ExchangeType = "topic";
            });
            ep.ConfigureDeadLetterQueueErrorTransport();
            ep.ConfigureConsumer<OrderCancelledConsumer>(ctx);
        });
    });
});

builder.Build().Run();
