using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Data;

public static class KnowledgeIndexInitializer
{
    // Indexes every article. Program.cs calls it at startup for the in-memory vector store, which starts empty;
    // Azure AI Search keeps its index between runs, so index an article there when it is published or changed.
    public static async Task<int> InitializeKnowledgeIndexAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<VectorKnowledgeRepository>();

        var chunkCount = 0;
        foreach (var article in await dbContext.KnowledgeArticles.AsNoTracking().ToListAsync(cancellationToken))
        {
            chunkCount += await repository.IndexArticleAsync(article, cancellationToken: cancellationToken);
        }

        return chunkCount;
    }
}
