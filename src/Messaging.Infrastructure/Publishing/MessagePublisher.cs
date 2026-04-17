namespace Messaging.Infrastructure.Publishing;

using System.Text.Json;
using Messaging.Contracts;
using Messaging.Infrastructure.Serialization;
using Messaging.Infrastructure.Topology;
using RabbitMQ.Client;

/// <summary>
/// Publishes any IMessage to its RabbitMQ exchange.
/// Owns a single long-lived IChannel created lazily on first publish.
/// </summary>
public sealed class MessagePublisher : IAsyncDisposable
{
    // Lazy<Task<T>> is safe for concurrent callers: the factory runs exactly once
    // and all waiters share the same Task. No CancellationToken needed here because
    // channel creation is done once at startup — if the connection is not yet ready,
    // the publish will surface the error naturally.
    private readonly Lazy<Task<IChannel>> _channel;

    public MessagePublisher(IConnection connection)
    {
        _channel = new Lazy<Task<IChannel>>(
            () => connection.CreateChannelAsync());
    }

    /// <param name="replyTo">
    /// For IQuery messages only: the name of the caller's exclusive reply queue.
    /// Leave null for IEvent and ICommand.
    /// </param>
    public async Task PublishAsync<T>(
        T message,
        CancellationToken ct = default,
        string? replyTo = null)
        where T : IMessage
    {
        IChannel channel = await _channel.Value;

        (string exchange, string routingKey) = TopologyResolver.Resolve<T>();
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJsonOptions.Default);

        // CorrelationId is set by the caller on the message record.
        // It comes from Activity.Current.TraceId — see PublisherWorker for the pattern.
        // Mirror it into both the AMQP native CorrelationId property and the
        // x-correlation-id header so all consumer frameworks can access it.
        BasicProperties props = new()
        {
            ContentType   = "application/json",
            DeliveryMode  = DeliveryModes.Persistent,
            MessageId     = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId,
            ReplyTo       = replyTo,
            Timestamp     = new AmqpTimestamp(message.OccurredOn.ToUnixTimeSeconds()),
            Headers       = new Dictionary<string, object?>
            {
                ["x-message-type"]   = typeof(T).FullName,
                ["x-schema-version"] = (object)message.SchemaVersion,
                ["x-correlation-id"] = message.CorrelationId,
                ["x-retry-count"]    = (object)0,
            }
        };

        await channel.BasicPublishAsync(exchange, routingKey, false, props, body, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel.IsValueCreated)
            await (await _channel.Value).DisposeAsync();
    }
}
