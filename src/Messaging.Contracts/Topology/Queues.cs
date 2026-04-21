// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts.Topology;

public static class Queues
{
    // ── Event queues — one per (consumer service × event type) ───────────────
    // Each consumer gets its own independent copy of every event it subscribes to.

    public const string NServiceBusOrderPlaced = "nsb.orders.placed";
    public const string MassTransitOrderPlaced = "mt.orders.placed";
    public const string RmqClientOrderPlaced = "rmq.orders.placed";

    public const string NServiceBusOrderCancelled = "nsb.orders.cancelled";
    public const string MassTransitOrderCancelled = "mt.orders.cancelled";
    public const string RmqClientOrderCancelled = "rmq.orders.cancelled";

    // ── Command queues — ONE queue per command type, shared by all senders ───
    // Commands are point-to-point: only one service binds to this queue.

    public const string CancelOrder = "orders.commands.cancel";

    // ── Query queues — ONE queue per query type ──────────────────────────────
    // The reply goes to a transient exclusive queue named by the caller
    // via BasicProperties.ReplyTo — not declared here.

    public const string GetOrderStatus = "orders.queries.status";

    // ── Utility ──────────────────────────────────────────────────────────────
    public static string DeadLetterQueue(string queueName) => $"{queueName}.dlq";
}
