namespace ContosoSupportCopilot.Domain.Evaluation;

public record RagEvaluationMetrics
{
    // Deterministic metrics from 0 to 1. They need no model, so they run offline, in tests and in CI.
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double CitationUsefulnessScore { get; init; }

    // The share of sentences that mostly reuse the sources' words: a signal for off-topic sentences,
    // blind to a wrong number or a reversed instruction.
    public double SourceOverlapRatio { get; init; }

    // The Valid Citations metric. A refusal without sources is valid; any other answer must cite at least one
    // source, only sources it received, and end normally.
    public bool CitationsValid { get; init; } = true;

    // LLM-judge scores from 1 (poor) to 5 (excellent); null when no model judged the answer.
    public double? GroundednessScore { get; init; }
    public double? RelevanceScore { get; init; }
    public double? RetrievalScore { get; init; }
    public double? CompletenessScore { get; init; }

    // The activity's judge, from 0 (generic) to 1 (answers exactly what was asked).
    public double? AnswerFocusScore { get; init; }

    public IReadOnlyList<Guid> RetrievedArticleIds { get; init; } = Array.Empty<Guid>();
}
