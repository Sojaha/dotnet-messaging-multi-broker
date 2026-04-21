// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts.Topology;

public static class Exchanges
{
    /// <summary>Topic exchange — events fan out to N consumer queues.</summary>
    public const string OrderEvents = "orders.events";

    /// <summary>Direct exchange — commands routed to exactly one queue.</summary>
    public const string OrderCommands = "orders.commands";

    /// <summary>Direct exchange — queries routed to exactly one queue.</summary>
    public const string OrderQueries = "orders.queries";

    /// <summary>Global dead-letter exchange (direct).</summary>
    public const string DeadLetter = "dlx";
}
