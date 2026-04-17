// Orders/Events/OrderPlaced.cs
namespace Messaging.Contracts.Orders.Events;

using Messaging.Contracts.Versioning;

/// <summary>
/// Raised when an order has been successfully placed and persisted.
/// Consumed by: invoicing service, warehouse service, notification service.
/// </summary>
[ContractVersion(ContractVersions.OrderPlaced)]
public sealed record OrderPlaced(
    Guid           MessageId,
    string         CorrelationId,
    DateTimeOffset OccurredOn,
    int            SchemaVersion,
    Guid           OrderId,
    Guid           CustomerId,
    decimal        TotalAmount,
    string         Currency
) : IEvent;
