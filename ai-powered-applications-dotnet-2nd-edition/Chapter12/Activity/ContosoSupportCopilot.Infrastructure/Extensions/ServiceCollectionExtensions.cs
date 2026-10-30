using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Health;
using ContosoSupportCopilot.Infrastructure.Monitoring;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    // The source of the GenAI chat and embedding spans and metrics (Chapter 12).
    public const string AITelemetrySourceName = "ContosoSupportCopilot.AI";

    public static IServiceCollection AddAIProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AIProviderOptions>()
            .Bind(configuration.GetSection(AIProviderOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Prompts and answers stay out of spans unless Telemetry:EnableSensitiveData is true.
        var sensitiveData = configuration.GetValue("Telemetry:EnableSensitiveData", false);

        services.AddSingleton<AIClientFactory>();

        // The first middleware added is the outermost: every call passes the resilience pipeline from Chapter 4,
        // then the usage recorder and the telemetry. Each attempt of a retried call gets its own span; only the
        // attempt that answers reports usage, which the recorder stores once. The recorder is there when cost
        // monitoring is registered (Chapter 12).
        services.AddChatClient(sp => sp.GetRequiredService<AIClientFactory>().CreateChatClient())
            .UseKernelResilience()
            .Use((inner, sp) => sp.GetService<ModelUsageRecorder>() is { } recorder ? new UsageRecordingChatClient(inner, recorder) : inner)
            .UseOpenTelemetry(sourceName: AITelemetrySourceName, configure: telemetry => telemetry.EnableSensitiveData = sensitiveData)
            .UseLogging();
        services.AddEmbeddingGenerator(sp => sp.GetRequiredService<AIClientFactory>().CreateEmbeddingGenerator())
            .Use((inner, sp) => sp.GetService<ModelUsageRecorder>() is { } recorder ? new UsageRecordingEmbeddingGenerator(inner, recorder) : inner)
            .UseOpenTelemetry(sourceName: AITelemetrySourceName, configure: telemetry => telemetry.EnableSensitiveData = sensitiveData);
        services.AddSingleton<ISupportChatClient, SupportChatClient>();

        return services;
    }

    public static IServiceCollection AddSupportCopilotInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAIProvider(configuration);

        var connectionString = configuration.GetConnectionString("SupportDb") ?? "Data Source=contoso-support.db";
        var databaseProvider = configuration.GetValue("Database:Provider", DatabaseProvider.Sqlite);
        services.AddDbContext<SupportDbContext>(options =>
        {
            if (databaseProvider == DatabaseProvider.SqlServer)
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });
        services.AddScoped<ISupportContext, SqlSupportContext>();

        // Semantic Kernel registrations moved to AddSemanticKernelServices in Chapter 4,
        // and the agent coordinator to AddAgentServices in Chapter 10.
        services.AddSingleton<AIConnectivityValidator>();

        return services;
    }
}
