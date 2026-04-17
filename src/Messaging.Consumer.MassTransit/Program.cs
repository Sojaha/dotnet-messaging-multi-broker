using MassTransit;
using Messaging.Consumer.MassTransit;
using Messaging.Contracts.Topology;
using Messaging.ServiceDefaults;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
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
            ep.ConfigureConsumer<OrderPlacedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint(Queues.MassTransitOrderCancelled, ep =>
        {
            ep.Bind(Exchanges.OrderEvents, b =>
            {
                b.RoutingKey   = RoutingKeys.OrderCancelled;
                b.ExchangeType = "topic";
            });
            ep.ConfigureConsumer<OrderCancelledConsumer>(ctx);
        });
    });
});

builder.Build().Run();
