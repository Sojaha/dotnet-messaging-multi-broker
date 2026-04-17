// Versioning/ContractVersions.cs
namespace Messaging.Contracts.Versioning;

/// <summary>
/// Single source of truth for all schema version numbers.
/// Update here + on the [ContractVersion] attribute whenever a version increments.
/// See ADR-0008 and ADR-0010.
/// </summary>
public static class ContractVersions
{
    // Events
    public const int OrderPlaced    = 1;
    public const int OrderCancelled = 1;

    // Commands
    public const int CancelOrder    = 1;

    // Queries
    public const int GetOrderStatus = 1;
}
