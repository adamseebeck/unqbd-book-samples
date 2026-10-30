using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class CoreAIServiceExtensions
{
    public static IServiceCollection AddCoreAIServices(
        this IServiceCollection services)
    {
        services.AddOptions<TokenPricingOptions>()
            .BindConfiguration(TokenPricingOptions.SectionName);

        services.AddScoped<IIntentExtractor, IntentExtractor>();
        services.AddScoped<IDraftingService, DraftingService>();

        return services;
    }
}
