using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class ValidationServiceExtensions
{
    public static IServiceCollection AddPromptValidationServices(
        this IServiceCollection services)
    {
        services.AddResiliencePipeline(StructuredOutputPipeline.Name, (builder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<KernelResilienceOptions>>().Value;
            builder.AddStructuredOutputResilience(options);
        });

        services.AddSingleton<IOutputValidator<SupportIntent>, SupportIntentValidator>();
        services.AddSingleton<IOutputValidator<EscalationDraft>, EscalationDraftValidator>();
        services.AddSingleton<IValidatedPromptExecutor, ValidatedPromptExecutor>();

        // Registered after AddCoreAIServices, so it replaces Chapter 3's IntentExtractor.
        services.AddScoped<IIntentExtractor, ReliableIntentExtractor>();

        return services;
    }
}
