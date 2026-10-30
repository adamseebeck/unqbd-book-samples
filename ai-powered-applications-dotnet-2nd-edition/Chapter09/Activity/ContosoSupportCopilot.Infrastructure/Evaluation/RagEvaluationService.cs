using System.Globalization;
using ContosoSupportCopilot.Domain.Evaluation;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

public sealed class RagEvaluationService
{
    public const string JudgeKey = "judge";

    private readonly IRagOrchestrator _ragOrchestrator;
    private readonly IReadOnlyList<IEvaluator> _judges;
    private readonly IRetrievalOptimizer _retrievalOptimizer;
    private readonly IPromptRevisionStrategy _promptRevisionStrategy;
    private readonly EvaluationOptions _options;
    private readonly ReportingConfiguration _deterministic;
    private readonly ReportingConfiguration _judged;

    public RagEvaluationService(
        IRagOrchestrator ragOrchestrator,
        IEnumerable<IEvaluator> evaluators,
        [FromKeyedServices(JudgeKey)] IEnumerable<IEvaluator> judges,
        [FromKeyedServices(JudgeKey)] IChatClient judgeClient,
        IRetrievalOptimizer retrievalOptimizer,
        IPromptRevisionStrategy promptRevisionStrategy,
        IOptions<EvaluationOptions> options,
        IOptions<AIProviderOptions> aiOptions,
        ILogger<RagEvaluationService> logger)
    {
        _ragOrchestrator = ragOrchestrator;
        _judges = judges.ToList();
        _retrievalOptimizer = retrievalOptimizer;
        _promptRevisionStrategy = promptRevisionStrategy;
        _options = options.Value;

        // The LLM judges need a real model, so by default they run only with a provider other than Offline.
        UsesJudges = _options.UseJudges ?? aiOptions.Value.Provider != AIProvider.Offline;
        if (UsesJudges && (string.IsNullOrWhiteSpace(_options.JudgeDeployment) || _options.JudgeDeployment == aiOptions.Value.ChatDeployment))
        {
            logger.LogWarning(
                "The judges use the deployment that writes the answers. Set Evaluation:JudgeDeployment to a different, ideally stronger, model.");
        }

        // One execution per service instance; the suffix keeps two runs that start in the same second apart.
        var executionName = $"{DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture)}-{Guid.NewGuid().ToString("N")[..8]}";
        var deterministic = evaluators.ToList();

        // Results go to disk for the report. Answers without sources get the deterministic evaluators only;
        // with a model, the judges grade the rest, and their responses are cached for unchanged requests.
        _deterministic = DiskBasedReportingConfiguration.Create(
            storageRootPath: _options.StoragePath,
            evaluators: deterministic,
            executionName: executionName);
        _judged = UsesJudges
            ? DiskBasedReportingConfiguration.Create(
                storageRootPath: _options.StoragePath,
                evaluators: [.. deterministic, .. _judges],
                chatConfiguration: new ChatConfiguration(judgeClient),
                enableResponseCaching: true,
                executionName: executionName)
            : _deterministic;
    }

    public bool UsesJudges { get; }

    public async Task<EvaluationReport> EvaluateDatasetAsync(
        EvaluationDataset dataset,
        PromptTemplate promptTemplate,
        VectorSearchRequest searchRequest,
        string variant,
        CancellationToken cancellationToken = default)
    {
        if (dataset.TestCases.Count == 0)
        {
            throw new ArgumentException("The dataset has no test cases.", nameof(dataset));
        }

        var results = new List<RagEvaluationResult>();
        foreach (var testCase in dataset.TestCases)
        {
            results.Add(await EvaluateTestCaseAsync(dataset, testCase, promptTemplate, searchRequest, variant, cancellationToken));
        }

        // Tune retrieval from the averages, so a single unusual query does not drive the change. The index was
        // built with the default chunking options (Chapter 7's initializer passes none), so those are current.
        var average = Average(results);
        var recommendation = await _retrievalOptimizer.OptimizeAsync(average, new ChunkingOptions(), searchRequest, cancellationToken);
        // Prompt problems show only in answers the model wrote; a refusal without sources is a retrieval problem.
        var weakest = results
            .Where(r => r.RequiresRevision && r.Metrics.RetrievedArticleIds.Count > 0)
            .OrderBy(r => r.Metrics.GroundednessScore ?? 5)
            .ThenBy(r => r.Metrics.CitationsValid)
            .ThenBy(r => r.Metrics.SourceOverlapRatio)
            .ThenBy(r => r.Metrics.CitationUsefulnessScore)
            .FirstOrDefault();
        var revised = weakest is null ? promptTemplate : await _promptRevisionStrategy.ReviseAsync(promptTemplate, weakest, cancellationToken);

        return new EvaluationReport
        {
            DatasetName = dataset.Name,
            Variant = variant,
            AverageMetrics = average,
            TotalTests = results.Count,
            FailedTests = results.Count(r => r.RequiresRevision),
            Results = results,
            RetrievalRecommendation = recommendation.ChangesRecommended ? recommendation : null,
            SuggestedPromptTemplate = ReferenceEquals(revised, promptTemplate) ? null : revised
        };
    }

    // Writes an HTML report of this run; `dotnet aieval report` builds the same report from the stored results.
    public async Task<int> WriteHtmlReportAsync(string reportPath, CancellationToken cancellationToken = default)
    {
        var results = new List<ScenarioRunResult>();
        await foreach (var result in _deterministic.ResultStore.ReadResultsAsync(_deterministic.ExecutionName, cancellationToken: cancellationToken))
        {
            results.Add(result);
        }

        await new HtmlReportWriter(reportPath).WriteReportAsync(results, cancellationToken);
        return results.Count;
    }

    private async Task<RagEvaluationResult> EvaluateTestCaseAsync(
        EvaluationDataset dataset,
        EvaluationTestCase testCase,
        PromptTemplate promptTemplate,
        VectorSearchRequest searchRequest,
        string variant,
        CancellationToken cancellationToken)
    {
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            Subject = testCase.Query,
            Description = testCase.Query,
            ProductArea = testCase.ProductArea,
            CreatedAt = DateTime.UtcNow
        };
        var request = new RagRequest
        {
            TicketId = ticket.Id,
            Query = testCase.Query,
            ProductArea = testCase.ProductArea,
            RequesterId = _options.RequesterId,
            MaxResults = searchRequest.TopK,
            MinRelevanceScore = searchRequest.MinScore,
            Prompt = promptTemplate
        };

        var rag = await _ragOrchestrator.GenerateGroundedResponseAsync(request, ticket, cancellationToken);
        var sources = rag.Citations.Select(c => c.Content).ToList();

        // The judges grade answers that a model wrote from sources. A refusal without sources is the
        // orchestrator's fixed text, so only the deterministic evaluators apply to it.
        var judged = UsesJudges && sources.Count > 0;
        List<EvaluationContext> contexts = [new RagEvaluationContext(rag, testCase.RelevantArticleIds)];
        if (judged)
        {
            contexts.Add(new GroundednessEvaluatorContext(string.Join("\n\n", sources)));
            contexts.Add(new RetrievalEvaluatorContext(sources));
            contexts.Add(new CompletenessEvaluatorContext(testCase.ExpectedAnswer));
        }

        // One scenario per test case and one iteration per variant, so the report compares variants side by side.
        var reporting = judged ? _judged : _deterministic;
        await using var scenario = await reporting.CreateScenarioRunAsync($"{dataset.Name}.{testCase.Name}", variant, cancellationToken: cancellationToken);
        var evaluation = await scenario.EvaluateAsync(
            [new ChatMessage(ChatRole.User, testCase.Query)],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, rag.Response)) { ModelId = rag.ModelId },
            contexts,
            cancellationToken);

        if (judged)
        {
            EnsureEveryJudgeScored(evaluation, testCase.Name);
        }

        var metrics = new RagEvaluationMetrics
        {
            Precision = Value(evaluation, RetrievalPrecisionEvaluator.PrecisionMetricName) ?? 0,
            Recall = Value(evaluation, RetrievalPrecisionEvaluator.RecallMetricName) ?? 0,
            CitationUsefulnessScore = Value(evaluation, CitationUsefulnessEvaluator.UsefulnessMetricName) ?? 0,
            SourceOverlapRatio = Value(evaluation, SourceOverlapEvaluator.SourceOverlapMetricName) ?? 0,
            CitationsValid = evaluation.TryGet<BooleanMetric>(CitationUsefulnessEvaluator.ValidityMetricName, out var valid) && valid.Value == true,
            GroundednessScore = Value(evaluation, GroundednessEvaluator.GroundednessMetricName),
            RelevanceScore = Value(evaluation, RelevanceEvaluator.RelevanceMetricName),
            RetrievalScore = Value(evaluation, RetrievalEvaluator.RetrievalMetricName),
            CompletenessScore = Value(evaluation, CompletenessEvaluator.CompletenessMetricName),
            AnswerFocusScore = Value(evaluation, AnswerFocusEvaluator.AnswerFocusMetricName),
            RetrievedArticleIds = rag.Citations.Select(c => c.ArticleId).Distinct().ToList()
        };

        var result = new RagEvaluationResult
        {
            TestCaseName = testCase.Name,
            Query = testCase.Query,
            Response = rag.Response,
            Metrics = metrics,
            LowOverlapSentences = SourceOverlapEvaluator.FindLowOverlapSentences(rag.Response, sources)
        };

        return result with { RevisionSuggestions = Suggestions(result, citedSources: rag.CitedArticleIds.Count > 0) };
    }

    // A judge call that failed (a rejected request, a reply cut short, text without a score) leaves its
    // metric without a value. Stop the run: averaging around a missing score would let a failure pass.
    private void EnsureEveryJudgeScored(EvaluationResult evaluation, string testCaseName)
    {
        foreach (var metricName in _judges.SelectMany(judge => judge.EvaluationMetricNames))
        {
            evaluation.Metrics.TryGetValue(metricName, out var metric);
            var error = metric?.Diagnostics?.FirstOrDefault(d => d.Severity == EvaluationDiagnosticSeverity.Error);
            if (metric is not NumericMetric { Value: not null } || error is not null)
            {
                // The diagnostic quotes the judge's reply over several lines: one line, at most 300 characters.
                var detail = string.Join(' ', (error?.Message ?? "no score.").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                throw new InvalidOperationException(
                    $"The {metricName} judge returned no score for test case '{testCaseName}': {(detail.Length > 300 ? detail[..300] + "..." : detail)} " +
                    "The stored results show the judge's full response.");
            }
        }
    }

    private static double? Value(EvaluationResult evaluation, string metricName) =>
        evaluation.TryGet<NumericMetric>(metricName, out var metric) ? metric.Value : null;

    // A retrieval miss scores 0 for precision and citation usefulness too, so those two suggestions need
    // retrieved articles and an answer that cited one; otherwise the recall suggestion is the one that fits.
    private static IReadOnlyList<string> Suggestions(RagEvaluationResult result, bool citedSources)
    {
        var suggestions = new List<string>();
        if (result.Metrics.SourceOverlapRatio < 0.8 || result.Metrics.GroundednessScore < 4)
        {
            suggestions.Add("Add explicit grounding instructions to the prompt template");
        }

        if (result.Metrics.Recall < 0.7)
        {
            suggestions.Add("Increase TopK or lower MinRelevanceScore to retrieve more candidate chunks");
        }

        if (result.Metrics.Precision < 0.6 && result.Metrics.RetrievedArticleIds.Count > 0)
        {
            suggestions.Add("Raise MinRelevanceScore or lower TopK to leave out irrelevant chunks");
        }

        if (!result.Metrics.CitationsValid)
        {
            suggestions.Add("Tell the model to cite only the articles in the prompt");
        }

        if (result.Metrics.CitationUsefulnessScore < 0.7 && citedSources)
        {
            suggestions.Add("Reduce chunk size to improve citation granularity");
        }

        return suggestions;
    }

    private static RagEvaluationMetrics Average(IReadOnlyList<RagEvaluationResult> results) => new()
    {
        Precision = results.Average(r => r.Metrics.Precision),
        Recall = results.Average(r => r.Metrics.Recall),
        CitationUsefulnessScore = results.Average(r => r.Metrics.CitationUsefulnessScore),
        SourceOverlapRatio = results.Average(r => r.Metrics.SourceOverlapRatio),
        CitationsValid = results.All(r => r.Metrics.CitationsValid),
        GroundednessScore = results.Average(r => r.Metrics.GroundednessScore),
        RelevanceScore = results.Average(r => r.Metrics.RelevanceScore),
        RetrievalScore = results.Average(r => r.Metrics.RetrievalScore),
        CompletenessScore = results.Average(r => r.Metrics.CompletenessScore),
        AnswerFocusScore = results.Average(r => r.Metrics.AnswerFocusScore)
    };
}
