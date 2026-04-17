// Versioning/ContractVersionValidator.cs
namespace Messaging.Contracts.Versioning;

using System.Reflection;

/// <summary>
/// Startup guard: fails fast if any IMessage implementation is missing
/// a [ContractVersion] attribute. Call before host.Run() in every service.
/// </summary>
public static class ContractVersionValidator
{
    public static void AssertAllVersioned()
    {
        var violations = typeof(IMessage).Assembly
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
