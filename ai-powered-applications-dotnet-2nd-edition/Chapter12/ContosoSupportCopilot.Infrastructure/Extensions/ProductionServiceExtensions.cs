using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Health;
using ContosoSupportCopilot.Infrastructure.Monitoring;
using ContosoSupportCopilot.Infrastructure.Performance;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class ProductionServiceExtensions
{
    // Readiness checks the dependencies; Program.cs maps liveness without any (Chapter 12).
    public static IServiceCollection AddProductionHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HealthOptions>()
            .Bind(configuration.GetSection(HealthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One instance: the background service that probes the AI provider, and the status that readiness reads.
        services.AddSingleton<AIProviderHealthMonitor>();
        services.AddHostedService(sp => sp.GetRequiredService<AIProviderHealthMonitor>());

        // A check that hangs reports Unhealthy after 2 seconds, inside the probe's timeoutSeconds of 3.
        services.AddHealthChecks().AddCheck<CompositeHealthCheck>("dependencies", tags: ["ready"], timeout: TimeSpan.FromSeconds(2));
        return services;
    }

    public static IServiceCollection AddCostMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BudgetOptions>()
            .Bind(configuration.GetSection(BudgetOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // With the recorder registered, AddAIProvider's chat client and embedding generator store every call's usage.
        services.AddSingleton<ModelUsageRecorder>();
        services.AddScoped<CostMonitoringService>();
        return services;
    }

    // Call after AddVectorSearchServices and AddContosoTelemetry: the cache replaces their IEmbeddingClient.
    public static IServiceCollection AddPerformanceOptimization(this IServiceCollection services, IConfiguration configuration)
    {
        var maxEntries = configuration.GetValue("EmbeddingCache:MaxEntries", 10_000);
        services.AddScoped<WorkflowBenchmark>();

        // A hand-written decorator: the cache wraps the Chapter 7 client, and every consumer gets the cache.
        services.AddSingleton<SupportEmbeddingClient>();
        services.AddSingleton<IEmbeddingClient>(sp => new CachingEmbeddingClient(
            sp.GetRequiredService<SupportEmbeddingClient>(),
            sp.GetRequiredService<ITelemetryCollector>(),
            sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId ?? "default",
            maxEntries));
        return services;
    }
}
