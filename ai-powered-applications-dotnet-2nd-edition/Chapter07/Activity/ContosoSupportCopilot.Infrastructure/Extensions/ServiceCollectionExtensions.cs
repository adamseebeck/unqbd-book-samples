using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Health;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AIProviderOptions>()
            .Bind(configuration.GetSection(AIProviderOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<AIClientFactory>();

        // The first middleware added is the outermost: every call, Semantic Kernel's too, passes
        // the resilience pipeline that AddSemanticKernelServices registers, then the logging.
        services.AddChatClient(sp => sp.GetRequiredService<AIClientFactory>().CreateChatClient())
            .UseKernelResilience()
            .UseLogging();
        services.AddEmbeddingGenerator(sp => sp.GetRequiredService<AIClientFactory>().CreateEmbeddingGenerator());
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

        // Semantic Kernel registrations moved to AddSemanticKernelServices in Chapter 4.
        services.AddSingleton<IAgentCoordinator, AgentFrameworkCoordinator>();
        services.AddSingleton<AIConnectivityValidator>();

        return services;
    }
}
