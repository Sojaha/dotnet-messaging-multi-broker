namespace Messaging.Infrastructure.Topology;

using System.Reflection;
using Messaging.Contracts;

/// <summary>Maps the x-message-type header string back to a CLR type.</summary>
public static class MessageTypeRegistry
{
    private static readonly Dictionary<string, Type> _registry;

    static MessageTypeRegistry()
    {
        _registry = typeof(IMessage).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMessage).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t);
    }

    public static Type Resolve(string? typeName)
        => typeName is not null && _registry.TryGetValue(typeName, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown message type header: {typeName}");
}
