using ContosoSupportCopilot.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// Gives the custom evaluators what the RAG pipeline retrieved and what the test case expected.
// The text contents name each source, so the evaluation report shows what the answer was based on.
public sealed class RagEvaluationContext : EvaluationContext
{
    public const string ContextName = "RAG sources";

    public RagEvaluationContext(RagResult result, IReadOnlyList<Guid> relevantArticleIds)
        : base(ContextName, result.Citations.Select(c => (AIContent)new TextContent($"{c.ArticleTitle}, chunk {c.ChunkIndex}: {c.Content}")))
    {
        Result = result;
        RelevantArticleIds = relevantArticleIds;
    }

    public RagResult Result { get; }
    public IReadOnlyList<Guid> RelevantArticleIds { get; }

    public static RagEvaluationContext From(IEnumerable<EvaluationContext>? additionalContext, string evaluatorName) =>
        additionalContext?.OfType<RagEvaluationContext>().FirstOrDefault()
            ?? throw new ArgumentException($"{evaluatorName} needs a {nameof(RagEvaluationContext)}.", nameof(additionalContext));
}
