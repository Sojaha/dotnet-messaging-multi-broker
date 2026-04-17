namespace Messaging.Infrastructure.Publishing;

using System.Diagnostics;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Infrastructure.Serialization;
using Messaging.Infrastructure.Topology;
using RabbitMQ.Client;

public sealed class MessagePublisher(IChannel channel)
{
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
        var (exchange, routingKey) = TopologyResolver.Resolve<T>();
        var body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJsonOptions.Default);

        // CorrelationId is set by the caller on the message record.
        // It comes from Activity.Current.TraceId — see PublisherWorker for the pattern.
        // Mirror it into both the AMQP native CorrelationId property and the
        // x-correlation-id header so all consumer frameworks can access it.
        var props = new BasicProperties
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
}
