using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// Compares the articles the pipeline retrieved with the ones the test case marks as relevant.
public sealed class RetrievalPrecisionEvaluator : IEvaluator
{
    public const string PrecisionMetricName = "Retrieval Precision";
    public const string RecallMetricName = "Retrieval Recall";

    public IReadOnlyCollection<string> EvaluationMetricNames => [PrecisionMetricName, RecallMetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = RagEvaluationContext.From(additionalContext, nameof(RetrievalPrecisionEvaluator));
        var retrieved = context.Result.Citations.Select(c => c.ArticleId).Distinct().ToList();
        var relevant = context.RelevantArticleIds;
        var hits = retrieved.Count(relevant.Contains);

        // With nothing relevant to find, retrieving nothing is perfect and anything retrieved is noise.
        double precision = retrieved.Count == 0 ? (relevant.Count == 0 ? 1.0 : 0.0) : (double)hits / retrieved.Count;
        double recall = relevant.Count == 0 ? 1.0 : (double)hits / relevant.Count;

        var precisionMetric = new NumericMetric(PrecisionMetricName, precision, $"{hits} of {retrieved.Count} retrieved articles are relevant.");
        var recallMetric = new NumericMetric(RecallMetricName, recall, $"{hits} of {relevant.Count} relevant articles were retrieved.");
        Interpret(precisionMetric, 0.6);
        Interpret(recallMetric, 0.7);

        return new ValueTask<EvaluationResult>(new EvaluationResult(precisionMetric, recallMetric));
    }

    internal static void Interpret(NumericMetric metric, double threshold) =>
        metric.Interpretation = metric.Value >= threshold
            ? new EvaluationMetricInterpretation(EvaluationRating.Good)
            : new EvaluationMetricInterpretation(EvaluationRating.Poor, failed: true, reason: $"{metric.Name} is below {threshold}.");
}
