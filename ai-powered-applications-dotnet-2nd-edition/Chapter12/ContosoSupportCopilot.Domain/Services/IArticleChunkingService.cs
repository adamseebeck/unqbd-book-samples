using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IArticleChunkingService
{
    Task<IReadOnlyList<ArticleChunk>> ChunkAsync(
        KnowledgeArticle article,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);
}
