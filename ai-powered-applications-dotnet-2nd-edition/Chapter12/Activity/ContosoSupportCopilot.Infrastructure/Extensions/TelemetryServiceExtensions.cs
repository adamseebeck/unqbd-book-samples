using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class TelemetryServiceExtensions
{
    // The sources that produce spans: chat and embedding calls (AddAIProvider), agents (Chapter 10)
    // and the application's own events.
    public static readonly string[] Sources =
    [
        ActivityTelemetryCollector.SourceName,
        ServiceCollectionExtensions.AITelemetrySourceName,
        SupportAgentFactory.TelemetrySourceName
    ];

    // Call after AddWorkflowServices and AddWorkflowOrchestration: the decorators replace their repositories.
    public static IServiceCollection AddContosoTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();

        // Telemetry:UseFileExporter writes the domain events to a JSON lines file instead of spans.
        if (configuration.GetValue("Telemetry:UseFileExporter", false))
        {
            services.AddOptions<FileTelemetryOptions>().Bind(configuration.GetSection(FileTelemetryOptions.SectionName));
            services.AddSingleton<ITelemetryCollector, FileTelemetryCollector>();
        }
        else
        {
            // The collector creates its meter through IMeterFactory, which the host registers; AddMetrics is a no-op then.
            services.AddMetrics();
            services.AddSingleton<ITelemetryCollector, ActivityTelemetryCollector>();
        }

        // A hand-written decorator: every stored run record also becomes a span and metrics.
        services.AddScoped<SqlAgentRunRepository>();
        services.AddScoped<IAgentRunRepository>(sp => new TelemetryAgentRunRepository(
            sp.GetRequiredService<SqlAgentRunRepository>(), sp.GetRequiredService<ITelemetryCollector>()));

        // The same pattern for the workflow: every saved step becomes a workflow_step span and a duration.
        services.AddScoped<SqlWorkflowStateRepository>();
        services.AddScoped<IWorkflowStateRepository>(sp => new TelemetryWorkflowStateRepository(
            sp.GetRequiredService<SqlWorkflowStateRepository>(), sp.GetRequiredService<ITelemetryCollector>()));

        if (options.Exporter == TelemetryExporter.None)
        {
            return services;
        }

        // One SDK registration for the whole app: the host owns and disposes the providers.
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName, serviceVersion: options.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing.AddSource(Sources).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (options.Exporter == TelemetryExporter.Otlp)
                {
                    tracing.AddOtlpExporter();
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                // "Polly" carries the resilience events of the Chapter 4 pipeline.
                metrics.AddMeter([.. Sources, "Polly"]).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (options.Exporter == TelemetryExporter.Otlp)
                {
                    metrics.AddOtlpExporter();
                }
                else
                {
                    metrics.AddConsoleExporter();
                }
            });

        if (options.Exporter == TelemetryExporter.Otlp)
        {
            telemetry.WithLogging(logging => logging.AddOtlpExporter());
        }

        return services;
    }
}
