// Orders/Events/OrderCancelled.cs
namespace Messaging.Contracts.Orders.Events;

using Messaging.Contracts.Versioning;

/// <summary>
/// Raised when an order has been cancelled for any reason.
/// Consumed by: invoicing service (void invoice), warehouse (release stock).
/// </summary>
[ContractVersion(ContractVersions.OrderCancelled)]
public sealed record OrderCancelled(
    Guid           MessageId,
    string         CorrelationId,
    DateTimeOffset OccurredOn,
    int            SchemaVersion,
    Guid           OrderId,
    string         Reason
) : IEvent;
