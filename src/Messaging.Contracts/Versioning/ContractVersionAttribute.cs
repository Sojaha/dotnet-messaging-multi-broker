// Versioning/ContractVersionAttribute.cs
namespace Messaging.Contracts.Versioning;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContractVersionAttribute(int version) : Attribute
{
    public int Version { get; } = version;
}
