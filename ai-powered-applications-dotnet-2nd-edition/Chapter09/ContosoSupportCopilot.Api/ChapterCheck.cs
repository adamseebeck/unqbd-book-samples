using System.Globalization;
using ContosoSupportCopilot.Domain.Evaluation;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    private const string Contradiction = "Send the password in the ticket reply";

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var chat = services.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        Console.WriteLine($"Contoso Support Copilot: Chapter 9 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");

        // The sample articles must be in the vector store; with Azure AI Search, this fills the index.
        await services.InitializeKnowledgeIndexAsync();

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var evaluation = provider.GetRequiredService<RagEvaluationService>();
        var evaluationOptions = provider.GetRequiredService<IOptions<EvaluationOptions>>().Value;
        var vectorOptions = provider.GetRequiredService<IOptions<VectorStoreOptions>>().Value;
        var dataset = SampleEvaluationData.CreateDataset();
        Console.WriteLine(evaluation.UsesJudges
            ? "LLM judges: Groundedness, Relevance, Retrieval and Completeness grade every answer that has sources."
            : "LLM judges: skipped, because they need a model. Precision, recall, citation usefulness and source overlap run offline.");
        Console.WriteLine($"Dataset {dataset.Name}: {dataset.TestCases.Count} test cases");

        // 1. A floor that is too strict for these embeddings, then the optimizer's suggestion.
        var strict = new VectorSearchRequest { TopK = 3, MinScore = 0.5 };
        var baseline = await evaluation.EvaluateDatasetAsync(dataset, SupportPrompts.GroundedAnswer, strict, "strict-floor");
        Print(baseline, strict);
        var suggestion = baseline.RetrievalRecommendation?.SuggestedSearchRequest;
        Console.WriteLine($"Optimizer: {baseline.RetrievalRecommendation?.Reasoning} (TopK {suggestion?.TopK}, MinScore {F(suggestion?.MinScore ?? 0)})");

        var candidate = await evaluation.EvaluateDatasetAsync(dataset, SupportPrompts.GroundedAnswer, suggestion ?? strict, "optimizer-suggestion");
        Print(candidate, suggestion ?? strict);

        // 2. The configured defaults from Chapter 7.
        var configured = new VectorSearchRequest { TopK = vectorOptions.TopK, MinScore = vectorOptions.MinScore };
        var defaults = await evaluation.EvaluateDatasetAsync(dataset, SupportPrompts.GroundedAnswer, configured, "configured");
        Print(defaults, configured);
        var missed = defaults.Results.Where(r => r.Metrics.Recall < 1).Select(r => r.TestCaseName);
        Console.WriteLine($"Still missed: {string.Join(", ", missed)}");

        // 3. What word overlap catches: a sentence that shares no words with its source.
        var refund = Source(SampleData.DuplicateChargeArticleId, "Refund a duplicate charge",
            "Duplicate charges usually come from a retried payment. Confirm both transactions in the payments dashboard, refund the later one, and tell the customer that the refund takes three to five business days.");
        var offTopic = await OverlapAsync(refund, $"Duplicate charges usually come from a retried payment {Cite(refund)}. Refunds arrive within one hour.");
        Console.WriteLine($"Source overlap for an off-topic sentence: {F(offTopic.Value ?? 0)}; {offTopic.Reason}");

        // 4. What it misses: a reversed instruction and a wrong number, built from the source's own words.
        var signIn = Source(SampleData.SignInErrorsArticleId, "Troubleshoot sign-in errors",
            "Never send a password or a reset code in a ticket reply. Reset links expire after one hour.");
        var contradiction = $"{Contradiction} {Cite(signIn)}. Reset links expire after 24 hours.";
        var faithful = $"Never send a password in a ticket reply {Cite(signIn)}. Reset links expire after one hour.";
        var overlap = await OverlapAsync(signIn, contradiction);
        Console.WriteLine($"Source overlap for a contradiction and a wrong number: {F(overlap.Value ?? 0)} ({(overlap.Interpretation!.Failed ? "failed" : "passed")})");

        // The groundedness judge reads meaning. Offline, a scripted judge returns the verdicts that a judge
        // model gives these two answers, through the same evaluator, judge client and score parsing.
        var judge = new ChatConfiguration(new JudgeChatClient(new OfflineChatClient([new ScriptedGroundednessJudge()]), evaluationOptions.JudgeMaxOutputTokens));
        var judgedContradiction = await GroundednessAsync(judge, signIn, contradiction);
        var judgedFaithful = await GroundednessAsync(judge, signIn, faithful);
        Console.WriteLine($"Groundedness from the scripted judge: contradiction {Score(judgedContradiction)}, faithful answer {Score(judgedFaithful)}");

        var flagged = new RagEvaluationResult
        {
            Response = contradiction,
            // Retrieval is perfect here, so only the overlap and the judge's score can call for a revision.
            Metrics = new RagEvaluationMetrics
            {
                Precision = 1,
                Recall = 1,
                SourceOverlapRatio = overlap.Value ?? 0,
                CitationUsefulnessScore = 1,
                GroundednessScore = judgedContradiction.Value
            },
            LowOverlapSentences = SourceOverlapEvaluator.FindLowOverlapSentences(contradiction, [signIn.Content])
        };
        var revised = await provider.GetRequiredService<IPromptRevisionStrategy>().ReviseAsync(SupportPrompts.GroundedAnswer, flagged);
        var added = revised.Instructions!.Split('\n').Except(SupportPrompts.GroundedAnswer.Instructions!.Split('\n'));
        Console.WriteLine($"Prompt revision: {revised.Name} {SupportPrompts.GroundedAnswer.Version} -> {revised.Version}, adds: {string.Join(" ", added)}");

        // 5. Comparing prompt versions needs a model: the offline responder ignores instructions.
        var variants = 3;
        if (evaluation.UsesJudges)
        {
            var revisedPrompt = defaults.SuggestedPromptTemplate ?? revised;
            var revisedRun = await evaluation.EvaluateDatasetAsync(dataset, revisedPrompt, configured, $"prompt-{revisedPrompt.Version}");
            Print(revisedRun, configured);
            Console.WriteLine($"Groundedness: prompt {SupportPrompts.GroundedAnswer.Version} {F(defaults.AverageMetrics.GroundednessScore ?? 0)}, prompt {revisedPrompt.Version} {F(revisedRun.AverageMetrics.GroundednessScore ?? 0)}");
            variants++;
        }
        else
        {
            Console.WriteLine($"Prompt variants: comparing {SupportPrompts.GroundedAnswer.Version} with {revised.Version} needs a model, because the offline responder ignores instructions.");
        }

        // 6. The stored results become an HTML report.
        var reportPath = Path.Combine(evaluationOptions.StoragePath, "report.html");
        var scenarioRuns = await evaluation.WriteHtmlReportAsync(reportPath);
        Console.WriteLine($"Report: {reportPath} ({scenarioRuns} scenario runs)");

        var passed = candidate.AverageMetrics.Recall > baseline.AverageMetrics.Recall
            && defaults.AverageMetrics.Recall >= candidate.AverageMetrics.Recall
            && defaults.Results.Single(r => r.TestCaseName == "out-of-scope").Response == RagOrchestrator.NoContentResponse
            && offTopic.Interpretation!.Failed
            && !overlap.Interpretation.Failed
            && judgedContradiction.Interpretation!.Failed
            && !judgedFaithful.Interpretation!.Failed
            && revised.Version != SupportPrompts.GroundedAnswer.Version
            && scenarioRuns == dataset.TestCases.Count * variants;
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    private static CitationReference Source(Guid articleId, string title, string content) =>
        new() { ArticleId = articleId, ArticleTitle = title, Content = content };

    private static string Cite(CitationReference source) => $"[Article: {source.ArticleTitle} ({source.ArticleId})]";

    private static async Task<NumericMetric> OverlapAsync(CitationReference source, string answer)
    {
        var rag = new RagResult { Response = answer, Citations = [source], CitedArticleIds = [source.ArticleId], IsGrounded = true };
        var result = await new SourceOverlapEvaluator().EvaluateAsync(
            [], new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)), additionalContext: [new RagEvaluationContext(rag, [source.ArticleId])]);
        return result.Get<NumericMetric>(SourceOverlapEvaluator.SourceOverlapMetricName);
    }

    private static async Task<NumericMetric> GroundednessAsync(ChatConfiguration judge, CitationReference source, string answer)
    {
        var result = await new GroundednessEvaluator().EvaluateAsync(
            [new ChatMessage(ChatRole.User, "Can I send the customer the new password in the ticket reply, and how long does a reset link last?")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)),
            judge,
            [new GroundednessEvaluatorContext(source.Content)]);
        return result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
    }

    private static string Score(NumericMetric metric) =>
        $"{metric.Value?.ToString("0", CultureInfo.InvariantCulture) ?? "none"} ({(metric.Interpretation?.Failed ?? true ? "failed" : "passed")})";

    private static void Print(EvaluationReport report, VectorSearchRequest search)
    {
        var m = report.AverageMetrics;
        Console.WriteLine($"{report.Variant} (TopK {search.TopK}, MinScore {F(search.MinScore)}): precision {F(m.Precision)}, recall {F(m.Recall)}, citation usefulness {F(m.CitationUsefulnessScore)}, source overlap {F(m.SourceOverlapRatio)}; {report.FailedTests} of {report.TotalTests} need revision");
        foreach (var result in report.Results)
        {
            Console.WriteLine($"  {result.TestCaseName}: precision {F(result.Metrics.Precision)}, recall {F(result.Metrics.Recall)}{(result.RevisionSuggestions.Count > 0 ? $"; {result.RevisionSuggestions[0]}" : string.Empty)}");
        }
    }

    private static string F(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    // Stands in for a judge model offline. It returns the verdict that a capable judge gives each of the two
    // answers above, in the tagged format that GroundednessEvaluator parses; it does not judge anything itself.
    private sealed class ScriptedGroundednessJudge : IOfflineResponder
    {
        public string? TryRespond(OfflineChatRequest request)
        {
            if (!request.UserText.Contains("**Groundedness**", StringComparison.Ordinal))
            {
                return null;
            }

            return request.UserText.Contains(Contradiction, StringComparison.Ordinal)
                ? "<S0>The context says never to send a password in a ticket reply, and that reset links expire after one hour.</S0>" +
                  "<S1>The response reverses the instruction and gives the wrong expiry.</S1><S2>1</S2>"
                : "<S0>Both sentences restate the context.</S0><S1>The response is fully supported by the context.</S1><S2>5</S2>";
        }
    }
}
