namespace Messaging.Infrastructure;

using Messaging.Contracts.Topology;
using RabbitMQ.Client;

/// <summary>
/// Declares all exchanges, queues, and DLQ bindings at startup.
/// - Events    → topic exchange (OrderEvents)
/// - Commands  → direct exchange (OrderCommands)
/// - Queries   → direct exchange (OrderQueries)
/// - DLX       → global direct exchange
///
/// Call once per service before consuming or publishing.
/// </summary>
public static class TopologyInitializer
{
    public static async Task DeclareAsync(IChannel channel, CancellationToken ct = default)
    {
        // Dead-letter exchange (direct, global)
        await channel.ExchangeDeclareAsync(Exchanges.DeadLetter, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Events — topic, fan-out to N consumer queues
        await channel.ExchangeDeclareAsync(Exchanges.OrderEvents, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Commands — direct, point-to-point
        await channel.ExchangeDeclareAsync(Exchanges.OrderCommands, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Queries — direct, point-to-point
        await channel.ExchangeDeclareAsync(Exchanges.OrderQueries, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: ct);
    }

    /// <summary>
    /// Declares a durable quorum queue with its DLQ binding.
    /// Call per queue from each consumer service at startup.
    /// </summary>
    public static async Task DeclareQueueAsync(
        IChannel channel,
        string queueName,
        string exchange,
        string routingKey,
        CancellationToken ct = default)
    {
        var dlqName = Queues.DeadLetterQueue(queueName);

        await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);
        await channel.QueueBindAsync(dlqName, Exchanges.DeadLetter, dlqName,
            cancellationToken: ct);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"]    = Exchanges.DeadLetter,
            ["x-dead-letter-routing-key"] = dlqName,
            ["x-queue-type"]              = "quorum",
        };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false,
            autoDelete: false, arguments: args, cancellationToken: ct);
        await channel.QueueBindAsync(queueName, exchange, routingKey,
            cancellationToken: ct);
    }
}
