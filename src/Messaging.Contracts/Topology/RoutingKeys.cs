// Topology/RoutingKeys.cs
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
