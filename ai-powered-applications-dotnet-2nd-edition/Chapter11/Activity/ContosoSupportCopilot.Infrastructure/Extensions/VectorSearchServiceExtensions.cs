using Azure;
using Azure.Search.Documents.Indexes;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class VectorSearchServiceExtensions
{
    // Call after AddSupportCopilotInfrastructure: the ISupportContext registration here replaces SqlSupportContext.
    // Embedding calls also need the "kernel" resilience pipeline that AddSemanticKernelServices registers.
    public static IServiceCollection AddVectorSearchServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<VectorStoreOptions>()
            .Bind(configuration.GetSection(VectorStoreOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The domain wrapper over the IEmbeddingGenerator that Chapter 2 registers for every provider.
        services.AddSingleton<IEmbeddingClient, SupportEmbeddingClient>();
        services.AddSingleton<IArticleChunkingService, ArticleChunkingService>();

        var provider = configuration.GetValue("VectorStore:Provider", VectorStoreProvider.InMemory);
        if (provider == VectorStoreProvider.AzureAISearch)
        {
            services.AddOptions<AzureAISearchOptions>()
                .Bind(configuration.GetSection(AzureAISearchOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // One client for the whole app: the Azure SDK reuses its HTTP connections.
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureAISearchOptions>>().Value;
                var endpoint = new Uri(options.Endpoint);
                return options.UseEntraId
                    ? new SearchIndexClient(endpoint, AzureCredentialFactory.Create(sp.GetRequiredService<IHostEnvironment>()))
                    : new SearchIndexClient(endpoint, new AzureKeyCredential(options.ApiKey!));
            });
            services.AddSingleton<IVectorSearchService, AzureAISearchVectorService>();
        }
        else
        {
            // The in-memory index must outlive every request, so it is a singleton.
            services.AddSingleton<IVectorSearchService, InMemoryVectorSearchService>();
        }

        // The repository decorates SqlSupportContext, so every ISupportContext consumer gets vector search.
        services.AddScoped(sp => new VectorKnowledgeRepository(
            new SqlSupportContext(sp.GetRequiredService<SupportDbContext>()),
            sp.GetRequiredService<IEmbeddingClient>(),
            sp.GetRequiredService<IVectorSearchService>(),
            sp.GetRequiredService<IArticleChunkingService>(),
            sp.GetRequiredService<IOptions<VectorStoreOptions>>(),
            sp.GetRequiredService<ILogger<VectorKnowledgeRepository>>()));
        services.AddScoped<ISupportContext>(sp => sp.GetRequiredService<VectorKnowledgeRepository>());

        return services;
    }
}
