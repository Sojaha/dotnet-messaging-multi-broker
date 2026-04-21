// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Versioning;

namespace Messaging.Contracts.Orders.Queries;

/// <summary>
/// Requests the current status of an order.
/// The handler replies with OrderStatusResult on the AMQP reply-to queue.
///
/// Routing: direct exchange orders.queries → queue orders.queries.status
/// Response: published to BasicProperties.ReplyTo with the same CorrelationId.
/// </summary>
[ContractVersion(ContractVersions.GetOrderStatus)]
public sealed record GetOrderStatus(
    Guid           MessageId,
    string         CorrelationId,
    DateTimeOffset OccurredOn,
    int            SchemaVersion,
    Guid           OrderId
) : IQuery<OrderStatusResult>;

/// <summary>
/// Reply payload for GetOrderStatus. Not an IMessage — it is a plain DTO
/// sent directly to the caller's reply queue, not published to an exchange.
/// </summary>
public sealed record OrderStatusResult(
    Guid   OrderId,
    string Status,       // e.g. "Pending", "Confirmed", "Cancelled", "Shipped"
    string CorrelationId // echoed back so the caller can match the response
);
