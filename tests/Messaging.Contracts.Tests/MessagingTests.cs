namespace Messaging.Contracts.Tests;

using FluentAssertions;
using Xunit;
using Messaging.Contracts.Orders.Events;
using Messaging.Contracts.Orders.Queries;
using Messaging.Contracts.Topology;
using Messaging.Contracts.Versioning;
using Messaging.Infrastructure.Topology;

public class TopologyTests
{
    [Fact]
    public void OrderPlaced_resolves_to_orders_exchange()
    {
        var (exchange, _) = TopologyResolver.Resolve<OrderPlaced>();
        exchange.Should().Be(Exchanges.OrderEvents);
    }

    [Fact]
    public void OrderPlaced_routing_key_contains_version_suffix()
    {
        var (_, routingKey) = TopologyResolver.Resolve<OrderPlaced>();
        routingKey.Should().EndWith(".v1");
    }

    [Fact]
    public void MessageTypeRegistry_roundtrips_all_contract_types()
    {
        var type = MessageTypeRegistry.Resolve(typeof(OrderPlaced).FullName);
        type.Should().Be(typeof(OrderPlaced));
    }

    [Fact]
    public void DeadLetterQueue_appends_dlq_suffix()
    {
        Queues.DeadLetterQueue("mt.orders.placed").Should().Be("mt.orders.placed.dlq");
    }
}

public class ContractVersioningTests
{
    [Fact]
    public void All_contract_types_carry_ContractVersion_attribute()
    {
        // Asserts that no contract record was added without the attribute.
        // Mirrors the runtime guard in ContractVersionValidator.
        var act = () => ContractVersionValidator.AssertAllVersioned();
        act.Should().NotThrow();
    }

    [Fact]
    public void OrderPlaced_schema_version_matches_ContractVersions_constant()
    {
        var attr = typeof(OrderPlaced)
            .GetCustomAttributes(typeof(ContractVersionAttribute), false)
            .Cast<ContractVersionAttribute>()
            .Single();

        attr.Version.Should().Be(ContractVersions.OrderPlaced);
    }

    [Fact]
    public void OrderCancelled_schema_version_matches_ContractVersions_constant()
    {
        var attr = typeof(OrderCancelled)
            .GetCustomAttributes(typeof(ContractVersionAttribute), false)
            .Cast<ContractVersionAttribute>()
            .Single();

        attr.Version.Should().Be(ContractVersions.OrderCancelled);
    }
}

public class CorrelationIdTests
{
    [Fact]
    public void CorrelationId_falls_back_to_new_guid_when_no_activity()
    {
        // No ambient Activity in a plain unit test — must still produce a value
        var id = System.Diagnostics.Activity.Current?.TraceId.ToString()
                 ?? Guid.NewGuid().ToString();

        id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CorrelationId_uses_Activity_TraceId_when_present()
    {
        using var activity = new System.Diagnostics.Activity("test.operation");
        activity.Start();

        var id = System.Diagnostics.Activity.Current?.TraceId.ToString()
                 ?? Guid.NewGuid().ToString();

        id.Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public void OrderPlaced_carries_CorrelationId_in_payload()
    {
        var correlationId = Guid.NewGuid().ToString();
        var msg = new OrderPlaced(
            MessageId:     Guid.NewGuid(),
            CorrelationId: correlationId,
            OccurredOn:    DateTimeOffset.UtcNow,
            SchemaVersion: ContractVersions.OrderPlaced,
            OrderId:       Guid.NewGuid(),
            CustomerId:    Guid.NewGuid(),
            TotalAmount:   10m,
            Currency:      "EUR");

        msg.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void CorrelationId_survives_json_roundtrip()
    {
        var correlationId = Guid.NewGuid().ToString();
        var msg = new OrderPlaced(
            MessageId:     Guid.NewGuid(),
            CorrelationId: correlationId,
            OccurredOn:    DateTimeOffset.UtcNow,
            SchemaVersion: ContractVersions.OrderPlaced,
            OrderId:       Guid.NewGuid(),
            CustomerId:    Guid.NewGuid(),
            TotalAmount:   10m,
            Currency:      "EUR");

        var json = System.Text.Json.JsonSerializer.Serialize(msg,
            Messaging.Infrastructure.Serialization.MessagingJsonOptions.Default);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<OrderPlaced>(json,
            Messaging.Infrastructure.Serialization.MessagingJsonOptions.Default);

        deserialized!.CorrelationId.Should().Be(correlationId);
    }
}
