namespace Messaging.ServiceDefaults;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

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
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource("Messaging.*"));

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

        LoggerConfiguration logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System",    LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName);

        if (!string.IsNullOrWhiteSpace(seqUrl))
            logConfig = logConfig.WriteTo.Seq(seqUrl);

        // AddSerilog replaces the built-in logging providers with Serilog.
        // dispose: true — Serilog flushes and closes on app shutdown.
        builder.Services.AddSerilog(logConfig.CreateLogger(), dispose: true);

        return builder;
    }
}
