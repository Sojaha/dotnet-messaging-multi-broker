// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Messaging.Contracts.Versioning;

/// <summary>
/// Startup guard: fails fast if any IMessage implementation is missing
/// a [ContractVersion] attribute. Call before host.Run() in every service.
/// </summary>
public static class ContractVersionValidator
{
    public static void AssertAllVersioned()
    {
        List<string> violations = typeof(IMessage).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMessage).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<ContractVersionAttribute>() is null)
            .Select(t => t.FullName!)
            .ToList();

        if (violations.Count > 0)
            throw new InvalidOperationException(
                "The following contract types are missing [ContractVersion]: " +
                string.Join(", ", violations));
    }
}
