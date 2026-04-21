// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Messaging.Contracts.Versioning;

namespace Messaging.Contracts.Orders.Commands;

/// <summary>
/// Instructs the order service to cancel a specific order.
/// Exactly ONE consumer handles this — the order service.
/// After processing, the handler publishes an OrderCancelled event.
///
/// Routing: direct exchange orders.commands → queue orders.commands.cancel
/// </summary>
[ContractVersion(ContractVersions.CancelOrder)]
public sealed record CancelOrder(
    Guid           MessageId,
    string         CorrelationId,
    DateTimeOffset OccurredOn,
    int            SchemaVersion,
    Guid           OrderId,
    string         RequestedBy,
    string         Reason
) : ICommand;
