using ContosoSupportCopilot.Domain.Prompts;

namespace ContosoSupportCopilot.Domain.Evaluation;

public interface IPromptRevisionStrategy
{
    Task<PromptTemplate> ReviseAsync(
        PromptTemplate currentTemplate,
        RagEvaluationResult evaluationResult,
        CancellationToken cancellationToken = default);
}

public class PromptRevisionStrategy : IPromptRevisionStrategy
{
    public const string GroundingRule =
        "Base every sentence on the articles. If information is missing, say so rather than inferring it.";

    public const string CitationRule =
        "Put a citation right after each fact, and cite only an article whose text states that fact.";

    public Task<PromptTemplate> ReviseAsync(
        PromptTemplate currentTemplate,
        RagEvaluationResult evaluationResult,
        CancellationToken cancellationToken = default)
    {
        if (!evaluationResult.RequiresRevision)
        {
            return Task.FromResult(currentTemplate);
        }

        // The rules belong in the instructions, which the model receives as its system prompt.
        var current = currentTemplate.Instructions ?? string.Empty;
        var instructions = current;
        if (evaluationResult.Metrics.SourceOverlapRatio < 0.8 || evaluationResult.Metrics.GroundednessScore < 4)
        {
            instructions = AddRule(instructions, GroundingRule);
        }

        if (evaluationResult.LowOverlapSentences.Count > 0 || evaluationResult.Metrics.CitationUsefulnessScore < 0.7
            || !evaluationResult.Metrics.CitationsValid)
        {
            instructions = AddRule(instructions, CitationRule);
        }

        // Rules that the template already has change nothing, so it keeps its version.
        if (instructions == current)
        {
            return Task.FromResult(currentTemplate);
        }

        // A new version, so evaluation results show which prompt produced which answers.
        return Task.FromResult(currentTemplate with
        {
            Instructions = instructions,
            Version = IncrementVersion(currentTemplate.Version),
            Description = $"{currentTemplate.Description} (revised after evaluation)"
        });
    }

    // Each rule appears once, however often the strategy runs.
    private static string AddRule(string instructions, string rule) =>
        instructions.Contains(rule, StringComparison.Ordinal) ? instructions : $"{instructions.TrimEnd()}\n{rule}";

    private static string IncrementVersion(string version) =>
        Version.TryParse(version, out var parsed) ? new Version(parsed.Major, parsed.Minor + 1).ToString() : "1.1";
}
