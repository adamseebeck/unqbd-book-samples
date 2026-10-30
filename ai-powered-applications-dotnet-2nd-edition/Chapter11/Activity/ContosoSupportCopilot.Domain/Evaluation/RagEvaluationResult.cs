namespace ContosoSupportCopilot.Domain.Evaluation;

public record RagEvaluationResult
{
    public string TestCaseName { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public string Response { get; init; } = string.Empty;
    public RagEvaluationMetrics Metrics { get; init; } = new();
    public IReadOnlyList<string> LowOverlapSentences { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RevisionSuggestions { get; init; } = Array.Empty<string>();
    public DateTime EvaluatedAt { get; init; } = DateTime.UtcNow;

    // Below these floors, revise the retrieval settings (precision, recall) or the prompt (an invalid citation,
    // low source overlap or citation usefulness, a low groundedness score). Only the groundedness judge catches
    // a sentence that contradicts its source; a missing judge score, because no model judged the answer, never fails.
    public bool RequiresRevision =>
        Metrics.Precision < 0.6 || Metrics.Recall < 0.7 || !Metrics.CitationsValid
        || Metrics.SourceOverlapRatio < 0.8 || Metrics.CitationUsefulnessScore < 0.7 || Metrics.GroundednessScore < 4;
}
