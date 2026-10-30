using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Tests.Fixtures;

// Returns the results a test adds, filtered like the real stores: product area, score floor, TopK.
public sealed class FakeVectorSearchService : IVectorSearchService
{
    private readonly List<VectorSearchResult> _results = [];

    public void AddResult(VectorSearchResult result) => _results.Add(result);

    public Task UpsertArticleChunksAsync(KnowledgeArticle article, IReadOnlyList<ArticleChunk> chunks, CancellationToken cancellationToken = default)
    {
        _results.RemoveAll(r => r.ArticleId == article.Id);
        _results.AddRange(chunks.Select(chunk => new VectorSearchResult
        {
            Chunk = chunk,
            Score = 1.0,
            ArticleId = article.Id,
            ArticleTitle = article.Title,
            ProductArea = article.ProductArea
        }));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryVector, VectorSearchRequest request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VectorSearchResult> matches = _results
            .Where(r => string.IsNullOrWhiteSpace(request.ProductAreaFilter) || string.Equals(r.ProductArea, request.ProductAreaFilter, StringComparison.Ordinal))
            .Where(r => r.Score >= request.MinScore)
            .OrderByDescending(r => r.Score)
            .Take(request.TopK)
            .ToList();
        return Task.FromResult(matches);
    }
}
