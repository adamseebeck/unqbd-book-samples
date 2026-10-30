using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Evaluation;

public interface IRetrievalOptimizer
{
    Task<OptimizationRecommendation> OptimizeAsync(
        RagEvaluationMetrics metrics,
        ChunkingOptions currentChunkingOptions,
        VectorSearchRequest currentSearchRequest,
        CancellationToken cancellationToken = default);
}

public record OptimizationRecommendation
{
    public ChunkingOptions? SuggestedChunkingOptions { get; init; }
    public VectorSearchRequest? SuggestedSearchRequest { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public bool ChangesRecommended => SuggestedChunkingOptions != null || SuggestedSearchRequest != null;
}

public class RetrievalOptimizer : IRetrievalOptimizer
{
    public Task<OptimizationRecommendation> OptimizeAsync(
        RagEvaluationMetrics metrics,
        ChunkingOptions currentChunkingOptions,
        VectorSearchRequest currentSearchRequest,
        CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();
        VectorSearchRequest? search = null;
        ChunkingOptions? chunking = null;

        // Small steps: a large change that helps one kind of query often harms another.
        // MinScore is a cosine similarity, so the steps stay between 0 and 0.9. A suggestion moves both
        // settings one way only, and when they are already at those limits, it suggests nothing.
        var topK = currentSearchRequest.TopK;
        var minScore = currentSearchRequest.MinScore;
        if (metrics.Recall < 0.7)
        {
            var looser = currentSearchRequest with
            {
                TopK = Math.Max(topK, Math.Min(topK + 3, 20)),
                MinScore = Math.Min(minScore, Math.Max(Math.Round(minScore - 0.05, 2), 0.0))
            };
            if (looser != currentSearchRequest)
            {
                search = looser;
                reasons.Add("Low recall suggests retrieving more chunks with a lower similarity floor");
            }
        }
        else if (metrics.Precision < 0.6)
        {
            var stricter = currentSearchRequest with
            {
                MinScore = Math.Max(minScore, Math.Min(Math.Round(minScore + 0.1, 2), 0.9)),
                TopK = Math.Min(topK, Math.Max(topK - 2, 3))
            };
            if (stricter != currentSearchRequest)
            {
                search = stricter;
                reasons.Add("Low precision suggests a stricter similarity floor");
            }
        }

        if (metrics.CitationUsefulnessScore < 0.6 && currentChunkingOptions.MaxChunkSize > 256)
        {
            chunking = currentChunkingOptions with
            {
                MaxChunkSize = (int)(currentChunkingOptions.MaxChunkSize * 0.8),
                OverlapSize = (int)(currentChunkingOptions.OverlapSize * 1.2)
            };
            reasons.Add("Poor citation usefulness suggests smaller chunks with more overlap");
        }

        return Task.FromResult(new OptimizationRecommendation
        {
            SuggestedSearchRequest = search,
            SuggestedChunkingOptions = chunking,
            Reasoning = string.Join("; ", reasons)
        });
    }
}
