// Topology/Exchanges.cs
namespace Messaging.Contracts.Topology;

public static class Exchanges
{
    /// <summary>Topic exchange — events fan out to N consumer queues.</summary>
    public const string OrderEvents   = "orders.events";

    /// <summary>Direct exchange — commands routed to exactly one queue.</summary>
    public const string OrderCommands = "orders.commands";

    /// <summary>Direct exchange — queries routed to exactly one queue.</summary>
    public const string OrderQueries  = "orders.queries";

    /// <summary>Global dead-letter exchange (direct).</summary>
    public const string DeadLetter    = "dlx";
}
