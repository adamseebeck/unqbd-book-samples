using ContosoSupportCopilot.Domain.Prompts;

namespace ContosoSupportCopilot.Domain.Evaluation;

public record EvaluationReport
{
    public string DatasetName { get; init; } = string.Empty;
    public string Variant { get; init; } = string.Empty;
    public RagEvaluationMetrics AverageMetrics { get; init; } = new();
    public int TotalTests { get; init; }
    public int FailedTests { get; init; }
    public IReadOnlyList<RagEvaluationResult> Results { get; init; } = Array.Empty<RagEvaluationResult>();
    public OptimizationRecommendation? RetrievalRecommendation { get; init; }
    public PromptTemplate? SuggestedPromptTemplate { get; init; }
}
