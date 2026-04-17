namespace Messaging.Infrastructure.Topology;

using Messaging.Contracts;
using Messaging.Contracts.Orders.Commands;
using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Orders.Queries;
using Messaging.Contracts.Topology;

public static class TopologyResolver
{
    private static readonly Dictionary<Type, (string Exchange, string RoutingKey)> _map = new()
    {
        // Events → topic exchange
        [typeof(OrderPlaced)]    = (Exchanges.OrderEvents,   RoutingKeys.OrderPlaced),
        [typeof(OrderCancelled)] = (Exchanges.OrderEvents,   RoutingKeys.OrderCancelled),

        // Commands → direct exchange
        [typeof(CancelOrder)]    = (Exchanges.OrderCommands, RoutingKeys.CancelOrder),

        // Queries → direct exchange
        [typeof(GetOrderStatus)] = (Exchanges.OrderQueries,  RoutingKeys.GetOrderStatus),
    };

    public static (string Exchange, string RoutingKey) Resolve<T>() where T : IMessage
        => Resolve(typeof(T));

    public static (string Exchange, string RoutingKey) Resolve(Type type)
        => _map.TryGetValue(type, out var v)
            ? v
            : throw new InvalidOperationException($"No topology registered for {type.FullName}");
}
