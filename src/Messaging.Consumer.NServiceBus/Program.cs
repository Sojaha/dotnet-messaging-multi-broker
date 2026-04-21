// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Messaging.Consumer.NServiceBus;
using Messaging.ServiceDefaults;
using Microsoft.Extensions.Hosting;
using NServiceBus;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

EndpointConfiguration endpointConfig = new("messaging.consumer.nsb");

TransportExtensions<RabbitMQTransport> transport = endpointConfig.UseTransport<RabbitMQTransport>();
transport.ConnectionString(builder.Configuration.GetConnectionString("rabbitmq")!);
transport.UseConventionalRoutingTopology(QueueType.Quorum);

// PropertyNameCaseInsensitive: our publisher serializes with camelCase (MessagingJsonOptions),
// NServiceBus SystemJsonSerializer defaults to PascalCase — case-insensitive handles both.
endpointConfig.UseSerialization<SystemJsonSerializer>()
    .Options(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
endpointConfig.EnableInstallers();
endpointConfig.EnableOpenTelemetry();

builder.UseNServiceBus(endpointConfig);

// Must be registered after UseNServiceBus so NSB starts first and declares its
// message-type exchanges before the bridge adds the E2E binding from orders.events.
builder.Services.AddHostedService<NsbTopologyBridge>();

builder.Build().Run();
