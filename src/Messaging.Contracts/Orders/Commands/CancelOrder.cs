// Orders/Commands/CancelOrder.cs
namespace Messaging.Contracts.Orders.Commands;

using Messaging.Contracts.Versioning;

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
