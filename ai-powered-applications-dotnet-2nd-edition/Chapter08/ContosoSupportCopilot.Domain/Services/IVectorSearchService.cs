using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IVectorSearchService
{
    // Replaces every chunk stored for the article, so a shorter new version leaves no old chunks behind.
    Task UpsertArticleChunksAsync(
        KnowledgeArticle article,
        IReadOnlyList<ArticleChunk> chunks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        VectorSearchRequest request,
        CancellationToken cancellationToken = default);
}
