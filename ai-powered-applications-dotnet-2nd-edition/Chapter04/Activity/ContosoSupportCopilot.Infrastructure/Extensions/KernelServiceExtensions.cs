using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Plugins;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Polly;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class KernelServiceExtensions
{
    public static IServiceCollection AddSemanticKernelServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KernelResilienceOptions>()
            .Bind(configuration.GetSection(KernelResilienceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The registry builds each named pipeline once, so every request shares its circuit breaker.
        services.AddResiliencePipeline(KernelResiliencePipeline.Name, (builder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<KernelResilienceOptions>>().Value;
            builder.AddKernelResilience(options);
        });

        // Semantic Kernel resolves the application's IChatClient from this container.
        services.AddKernel();

        // Scoped, because the plugin depends on the scoped ISupportContext.
        services.AddScoped(sp => KernelPluginFactory.CreateFromType<SupportTicketPlugin>("SupportTicketPlugin", sp));
        services.AddScoped(sp => KernelPluginFactory.CreateFromType<CustomerHistoryPlugin>("CustomerHistory", sp));
        services.AddScoped<IKernelOrchestrator, SemanticKernelOrchestrator>();

        return services;
    }
}
