using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class RagServiceExtensions
{
    // Needs AddVectorSearchServices: the orchestrator uses its embedding client, vector store and ISupportContext.
    public static IServiceCollection AddRagServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RagOptions>()
            .Bind(configuration.GetSection(RagOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Stateless detectors and builders serve every request.
        services.AddSingleton<PromptInjectionDetector>();
        services.AddSingleton<IContentSanitizer, ContentSanitizer>();
        services.AddSingleton<ISensitiveDataMasker, SensitiveDataMasker>();
        services.AddSingleton<RagPromptBuilder>();

        // Scoped, so each request gets its own cache of authorization decisions.
        services.AddScoped<ProductAreaAuthorizationFilter>();
        services.AddScoped<IAuthorizationFilter>(sp =>
            new CachingAuthorizationFilter(sp.GetRequiredService<ProductAreaAuthorizationFilter>()));
        services.AddScoped<IRagOrchestrator, RagOrchestrator>();

        // Offline answers cite the articles in the prompt, so grounding validation runs without a model.
        services.AddSingleton<IOfflineResponder, OfflineGroundedAnswerResponder>();

        return services;
    }
}
