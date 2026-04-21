// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Versioning;

namespace Messaging.Contracts.Orders.Events;

/// <summary>
/// Raised when an order has been cancelled for any reason.
/// Consumed by: invoicing service (void invoice), warehouse (release stock).
/// </summary>
[ContractVersion(ContractVersions.OrderCancelled)]
public sealed record OrderCancelled(
    Guid MessageId,
    string CorrelationId,
    DateTimeOffset OccurredOn,
    int SchemaVersion,
    Guid OrderId,
    string Reason
) : IEvent;
