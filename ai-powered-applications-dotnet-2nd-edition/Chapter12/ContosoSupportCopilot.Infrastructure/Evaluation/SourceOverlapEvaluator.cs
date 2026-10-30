using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// Flags answer sentences that share few words with the sources: a cheap signal for off-topic text.
// It cannot see meaning, so a wrong number or a reversed instruction built from the sources' words
// passes. The groundedness judge is the check for those.
public sealed class SourceOverlapEvaluator : IEvaluator
{
    public const string SourceOverlapMetricName = "Source Overlap";

    public IReadOnlyCollection<string> EvaluationMetricNames => [SourceOverlapMetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = RagEvaluationContext.From(additionalContext, nameof(SourceOverlapEvaluator));
        var sources = context.Result.Citations.Select(c => c.Content).ToList();
        var sentences = TextOverlap.Sentences(modelResponse.Text);
        var lowOverlap = FindLowOverlapSentences(modelResponse.Text, sources);

        // Without sources the pipeline answered with its fallback, which states no facts.
        double ratio = sources.Count == 0 || sentences.Count == 0 ? 1.0 : (double)(sentences.Count - lowOverlap.Count) / sentences.Count;
        var reason = lowOverlap.Count == 0
            ? $"All {sentences.Count} sentences mostly reuse words from the sources."
            : $"Low overlap: {string.Join(" | ", lowOverlap)}";

        var metric = new NumericMetric(SourceOverlapMetricName, ratio, reason);
        RetrievalPrecisionEvaluator.Interpret(metric, 0.8);
        return new ValueTask<EvaluationResult>(new EvaluationResult(metric));
    }

    public static IReadOnlyList<string> FindLowOverlapSentences(string answer, IReadOnlyList<string> sources)
    {
        if (sources.Count == 0)
        {
            return [];
        }

        var sourceWords = TextOverlap.ContentWords(string.Join(" ", sources));
        return TextOverlap.Sentences(answer)
            .Where(sentence => TextOverlap.Coverage(sentence, sourceWords) < 0.6)
            .ToList();
    }
}
