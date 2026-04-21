// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts.Versioning;

/// <summary>
/// Single source of truth for all schema version numbers.
/// Update here + on the [ContractVersion] attribute whenever a version increments.
/// See ADR-0008 and ADR-0010.
/// </summary>
public static class ContractVersions
{
    // Events
    public const int OrderPlaced = 1;
    public const int OrderCancelled = 1;

    // Commands
    public const int CancelOrder = 1;

    // Queries
    public const int GetOrderStatus = 1;
}
