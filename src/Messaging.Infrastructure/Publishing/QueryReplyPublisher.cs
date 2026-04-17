namespace Messaging.Infrastructure.Publishing;

using System.Text.Json;
using Messaging.Infrastructure.Serialization;
using RabbitMQ.Client;

/// <summary>
/// Sends a query reply back to the caller's exclusive reply queue.
/// The reply queue name comes from BasicProperties.ReplyTo on the inbound query.
/// The CorrelationId is echoed back so the caller can match the response.
/// </summary>
public sealed class QueryReplyPublisher(IChannel channel)
{
    public async Task ReplyAsync<TResult>(
        TResult result,
        string replyTo,
        string correlationId,
        CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(result, MessagingJsonOptions.Default);

        var props = new BasicProperties
        {
            ContentType   = "application/json",
            CorrelationId = correlationId,
            Headers = new Dictionary<string, object?>
            {
                ["x-correlation-id"] = correlationId,
            }
        };

        // Reply directly to the caller's transient queue — no exchange routing
        await channel.BasicPublishAsync(
            exchange:        string.Empty,
            routingKey:      replyTo,
            mandatory:       false,
            basicProperties: props,
            body:            body,
            cancellationToken: ct);
    }
}
