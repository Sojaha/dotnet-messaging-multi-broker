// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Topology;
using RabbitMQ.Client;

namespace Messaging.Consumer.NServiceBus;

/// <summary>
/// Creates the exchange-to-exchange binding that routes messages from the shared
/// "orders.events" topic exchange into the NServiceBus message-type fanout exchange.
///
/// NServiceBus ConventionalRoutingTopology declares a fanout exchange named after
/// each handled CLR type (e.g. "Messaging.Contracts.Orders.Events.OrderPlaced") and
/// binds the endpoint queue to it. Our publisher writes to "orders.events" with a
/// topic routing key. This service bridges the gap: once NSB has initialised its
/// exchanges, the E2E binding here ensures matching messages are forwarded.
///
/// Uses RabbitMQ.Client 6.x (the version NSB.RabbitMQ pins to internally) via
/// VersionOverride in the project file — the 7.x IChannel API is not available here.
/// </summary>
public sealed class NsbTopologyBridge(
    IConfiguration configuration,
    ILogger<NsbTopologyBridge> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        string connectionString = configuration.GetConnectionString("rabbitmq")
            ?? throw new InvalidOperationException("Missing 'rabbitmq' connection string");

        ConnectionFactory factory = new() { Uri = new Uri(connectionString) };
        using IConnection connection = factory.CreateConnection();
        using IModel channel = connection.CreateModel();

        // Declare source exchange idempotently — the publisher also declares it but may not
        // have started yet; ExchangeBind will fail with 404 if the source doesn't exist.
        channel.ExchangeDeclare(Exchanges.OrderEvents, ExchangeType.Topic, durable: true);

        // Declare destination exchange idempotently — NSB will also declare this when it
        // scans IHandleMessages<OrderPlaced>; identical args make it a no-op either way.
        string nsbExchange = typeof(OrderPlaced).FullName!;
        channel.ExchangeDeclare(nsbExchange, ExchangeType.Fanout, durable: true);

        // E2E binding: orders.events (topic) → NSB fanout exchange, topic-filtered by routing key
        channel.ExchangeBind(
            destination: nsbExchange,
            source:      Exchanges.OrderEvents,
            routingKey:  RoutingKeys.OrderPlaced);

        logger.LogInformation(
            "NSB topology bridge: {Source} --[{Key}]--> {Dest}",
            Exchanges.OrderEvents, RoutingKeys.OrderPlaced, nsbExchange);

        return Task.CompletedTask;
    }
}
