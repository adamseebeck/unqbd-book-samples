using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// Checks that the cited sources contributed to the answer, not only that citations are present.
public sealed class CitationUsefulnessEvaluator : IEvaluator
{
    public const string UsefulnessMetricName = "Citation Usefulness";
    public const string ValidityMetricName = "Valid Citations";

    public IReadOnlyCollection<string> EvaluationMetricNames => [UsefulnessMetricName, ValidityMetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = RagEvaluationContext.From(additionalContext, nameof(CitationUsefulnessEvaluator));
        var rag = context.Result;
        // Citations name articles, so usefulness is counted per cited article, over its retrieved chunks.
        var cited = rag.Citations.Where(c => rag.CitedArticleIds.Contains(c.ArticleId)).GroupBy(c => c.ArticleId).ToList();
        var sentences = TextOverlap.Sentences(modelResponse.Text);

        // A cited article is useful when at least one sentence of the answer is mostly made of its words.
        var useful = cited.Count(article =>
        {
            var articleWords = TextOverlap.ContentWords(string.Join(" ", article.Select(chunk => chunk.Content)));
            return sentences.Any(sentence => TextOverlap.Coverage(sentence, articleWords) >= 0.6);
        });

        // A refusal without sources is right when the test case expects no articles.
        double usefulness = rag.Citations.Count == 0
            ? (context.RelevantArticleIds.Count == 0 ? 1.0 : 0.0)
            : cited.Count == 0 ? 0.0 : (double)useful / cited.Count;

        var usefulnessMetric = new NumericMetric(UsefulnessMetricName, usefulness, $"{useful} of {cited.Count} cited articles contributed text to the answer.");
        RetrievalPrecisionEvaluator.Interpret(usefulnessMetric, 0.7);

        // A refusal without sources is valid. Any other answer must pass Chapter 8's IsGrounded: it cites at least
        // one source, only sources it received, and ended normally.
        var valid = rag.Citations.Count == 0 || rag.IsGrounded;
        var validityMetric = new BooleanMetric(ValidityMetricName, valid, valid
            ? "Every citation names a provided source."
            : "The answer had sources but cites none, cites an article it was not given, or stopped before it finished.");
        validityMetric.Interpretation = new EvaluationMetricInterpretation(valid ? EvaluationRating.Good : EvaluationRating.Unacceptable, failed: !valid);

        return new ValueTask<EvaluationResult>(new EvaluationResult(usefulnessMetric, validityMetric));
    }
}
