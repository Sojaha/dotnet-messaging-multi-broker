// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts.Topology;

public static class RoutingKeys
{
    // Events (topic exchange — supports wildcard bindings)
    public const string OrderPlaced    = "orders.placed.v1";
    public const string OrderCancelled = "orders.cancelled.v1";

    // Commands (direct exchange — exact match only)
    public const string CancelOrder    = "orders.cancel.v1";

    // Queries (direct exchange — exact match only)
    public const string GetOrderStatus = "orders.status.v1";
}
