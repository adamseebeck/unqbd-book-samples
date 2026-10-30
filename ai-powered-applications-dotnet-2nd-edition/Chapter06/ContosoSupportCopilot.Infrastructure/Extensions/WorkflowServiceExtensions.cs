using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Correlation;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class WorkflowServiceExtensions
{
    public static IServiceCollection AddWorkflowServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // The correlation ID lives in an AsyncLocal, so one instance serves every request.
        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IConversationRepository, SqlConversationRepository>();
        services.AddScoped<IAgentRunRepository, SqlAgentRunRepository>();

        // Copilot state uses its own database, next to the support database from Chapter 2.
        var connectionString = configuration.GetConnectionString("CopilotDb") ?? "Data Source=contoso-copilot.db";
        var databaseProvider = configuration.GetValue("Database:Provider", DatabaseProvider.Sqlite);
        services.AddDbContext<CopilotDbContext>(options =>
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

        services.AddOptions<FallbackOptions>()
            .Bind(configuration.GetSection(FallbackOptions.SectionName));
        services.AddMemoryCache();

        // The decorator: IDraftingService resolves to FallbackDraftingService around DraftingService.
        // This runs after AddCoreAIServices, so it replaces the Chapter 3 registration.
        services.AddScoped<DraftingService>();
        services.AddScoped<IDraftingService>(provider =>
        {
            var inner = provider.GetRequiredService<DraftingService>();
            var logger = provider.GetRequiredService<ILogger<FallbackDraftingService>>();
            var cache = provider.GetRequiredService<IMemoryCache>();
            var options = provider.GetRequiredService<IOptions<FallbackOptions>>();

            return new FallbackDraftingService(inner, logger, cache, options);
        });

        services.AddScoped<SupportWorkflowService>();

        return services;
    }
}
