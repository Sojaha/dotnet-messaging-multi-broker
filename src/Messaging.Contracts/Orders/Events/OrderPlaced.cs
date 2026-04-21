// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Versioning;

namespace Messaging.Contracts.Orders.Events;

/// <summary>
/// Raised when an order has been successfully placed and persisted.
/// Consumed by: invoicing service, warehouse service, notification service.
/// </summary>
[ContractVersion(ContractVersions.OrderPlaced)]
public sealed record OrderPlaced(
    Guid MessageId,
    string CorrelationId,
    DateTimeOffset OccurredOn,
    int SchemaVersion,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency
) : IEvent;
