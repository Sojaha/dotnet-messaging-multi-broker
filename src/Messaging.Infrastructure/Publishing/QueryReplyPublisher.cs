// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Infrastructure.Serialization;
using RabbitMQ.Client;
using System.Text.Json;

namespace Messaging.Infrastructure.Publishing;

/// <summary>
/// Sends a query reply back to the caller's exclusive reply queue.
/// The reply queue name comes from BasicProperties.ReplyTo on the inbound query.
/// The CorrelationId is echoed back so the caller can match the response.
///
/// The IChannel is passed per-call rather than injected: channels are created
/// async from IConnection and are not safe to register directly in the DI container.
/// Callers (e.g. RmqConsumerWorker) own the channel lifetime.
/// </summary>
public sealed class QueryReplyPublisher
{
    public async Task ReplyAsync<TResult>(
        TResult result,
        string replyTo,
        string correlationId,
        IChannel channel,
        CancellationToken ct = default)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(result, MessagingJsonOptions.Default);

        BasicProperties props = new()
        {
            ContentType = "application/json",
            CorrelationId = correlationId,
            Headers = new Dictionary<string, object?>
            {
                ["x-correlation-id"] = correlationId,
            }
        };

        // Reply directly to the caller's transient queue — no exchange routing
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: replyTo,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
