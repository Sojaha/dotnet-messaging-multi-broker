// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace Messaging.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.ConfigureSerilog();
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddMeter("Messaging")          // custom messaging metrics
                .AddMeter("MassTransit")        // MassTransit built-in metrics
                .AddMeter("NServiceBus.Core"))  // NServiceBus built-in metrics
            .WithTracing(tracing => tracing
                .AddSource("Messaging.Publishing")
                .AddSource("Messaging.Consuming")
                .AddSource("MassTransit")        // MassTransit built-in OTel
                .AddSource("NServiceBus.Core")); // NServiceBus built-in OTel

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(
        this IHostApplicationBuilder builder)
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureSerilog(
        this IHostApplicationBuilder builder)
    {
        // Aspire injects the Seq URL as a connection string named "seq".
        // Fall back to the SEQ_SERVERURL env var for non-Aspire runs.
        string? seqUrl = builder.Configuration.GetConnectionString("seq")
            ?? builder.Configuration["SEQ_SERVERURL"];

        // Aspire injects the OTLP endpoint for its dashboard.
        // We forward logs there so they appear in Aspire's structured-logs view.
        string? otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        LoggerConfiguration logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System",    LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            // Always write to the terminal — useful in both local dotnet run and Aspire.
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Application} {Message:lj}{NewLine}{Exception}");

        if (!string.IsNullOrWhiteSpace(seqUrl))
            logConfig = logConfig.WriteTo.Seq(seqUrl);

        // Send structured logs to the Aspire dashboard (or any OTLP collector).
        // IncludedData ensures trace/span IDs are attached so logs link to traces.
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            logConfig = logConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint           = otlpEndpoint;
                options.Protocol           = OtlpProtocol.Grpc;
                options.IncludedData       = IncludedData.TraceIdField | IncludedData.SpanIdField;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = builder.Environment.ApplicationName,
                };
            });
        }

        // AddSerilog replaces the built-in logging providers with Serilog.
        // dispose: true — Serilog flushes and closes on app shutdown.
        builder.Services.AddSerilog(logConfig.CreateLogger(), dispose: true);

        return builder;
    }
}
