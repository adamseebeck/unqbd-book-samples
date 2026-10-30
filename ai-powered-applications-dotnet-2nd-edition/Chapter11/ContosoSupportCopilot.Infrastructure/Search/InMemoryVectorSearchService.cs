using System.Collections.Concurrent;
using System.Numerics.Tensors;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Search;

// Keeps every chunk in memory and compares the query with each one: an exact search that is
// fast enough for a few thousand chunks and needs no service. The index is empty after a restart.
public sealed class InMemoryVectorSearchService : IVectorSearchService
{
    private readonly ConcurrentDictionary<Guid, StoredChunk[]> _chunksByArticle = new();

    public Task UpsertArticleChunksAsync(
        KnowledgeArticle article,
        IReadOnlyList<ArticleChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        // Replacing the whole array swaps the article's chunks in one step.
        _chunksByArticle[article.Id] = chunks
            .Select(chunk => new StoredChunk(chunk, article.Id, article.Title, article.ProductArea, article.IsPublished))
            .ToArray();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VectorSearchResult> results = _chunksByArticle.Values
            .SelectMany(chunks => chunks)
            .Where(stored => request.IncludeUnpublished || stored.IsPublished)
            // An empty filter matches every area; a value matches exactly, like an OData eq filter.
            .Where(stored => string.IsNullOrEmpty(request.ProductAreaFilter)
                || string.Equals(stored.ProductArea, request.ProductAreaFilter, StringComparison.Ordinal))
            .Select(stored => new VectorSearchResult
            {
                // 1 means the same direction; 0 means orthogonal. Vectors of different lengths throw here.
                Score = TensorPrimitives.CosineSimilarity(queryVector, stored.Chunk.Embedding),
                Chunk = stored.Chunk with { Embedding = [] },
                ArticleId = stored.ArticleId,
                ArticleTitle = stored.ArticleTitle,
                ProductArea = stored.ProductArea
            })
            .Where(result => result.Score >= request.MinScore)
            .OrderByDescending(result => result.Score)
            .Take(request.TopK)
            .ToList();

        return Task.FromResult(results);
    }

    private sealed record StoredChunk(
        ArticleChunk Chunk,
        Guid ArticleId,
        string ArticleTitle,
        string ProductArea,
        bool IsPublished);
}
