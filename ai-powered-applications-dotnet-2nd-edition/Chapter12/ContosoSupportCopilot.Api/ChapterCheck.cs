using System.Diagnostics;
using System.Globalization;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
using ContosoSupportCopilot.Infrastructure.Health;
using ContosoSupportCopilot.Infrastructure.Monitoring;
using ContosoSupportCopilot.Infrastructure.Performance;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var chat = services.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        Console.WriteLine($"Contoso Support Copilot: Chapter 12 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        var passed = true;

        // The host starts the OpenTelemetry providers when it runs; the check runs before that.
        var tracing = services.GetService<TracerProvider>();
        var metrics = services.GetService<MeterProvider>();
        Console.WriteLine($"OpenTelemetry exporter: {(tracing is null ? "none" : "on")}");

        // The sample articles must be in the vector store; with Azure AI Search, this fills the index.
        await services.InitializeKnowledgeIndexAsync();

        // 1. The spans an OpenTelemetry exporter receives, collected here with a listener.
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => TelemetryServiceExtensions.Sources.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (spans) { spans.Add(activity); } }
        };
        ActivitySource.AddActivityListener(listener);

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var result = await provider.GetRequiredService<ISupportWorkflow>()
            .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "Customer is locked out after a reset");
        Console.WriteLine($"Workflow: {result.FinalStep}, citations {result.Citations.Count}");

        foreach (var group in spans.GroupBy(s => $"{s.Source.Name}: {s.DisplayName}").OrderBy(g => g.Key))
        {
            Console.WriteLine($"Span {group.Key} x{group.Count()}");
        }

        var agentSpan = spans.FirstOrDefault(s => s.DisplayName.StartsWith("invoke_agent TriageAgent", StringComparison.Ordinal));
        Console.WriteLine($"invoke_agent TriageAgent: gen_ai.usage.input_tokens {agentSpan?.GetTagItem("gen_ai.usage.input_tokens")}, " +
            $"gen_ai.response.model {agentSpan?.GetTagItem("gen_ai.response.model")}");
        var contentCaptured = spans.Any(s => s.Tags.Any(t => t.Key is "gen_ai.input.messages" or "gen_ai.output.messages"));
        Console.WriteLine($"Prompt and answer text in spans: {contentCaptured}");
        passed &= agentSpan is not null && !contentCaptured && spans.Any(s => s.DisplayName.StartsWith("execute_tool", StringComparison.Ordinal))
            && spans.Any(s => s.DisplayName == "workflow_step Drafting");

        // 2. Readiness reads the AI status that the background monitor keeps. --check never starts the host's
        // background services, so the check runs one probe itself.
        Console.WriteLine($"AI provider probe: {await services.GetRequiredService<AIProviderHealthMonitor>().ProbeAsync()}");
        var health = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(check => check.Tags.Contains("ready"));
        Console.WriteLine($"Readiness: {health.Status}; " + string.Join(", ",
            health.Entries.SelectMany(e => e.Value.Data).Select(d => $"{d.Key} {d.Value}")));
        passed &= health.Status == HealthStatus.Healthy;

        // 3. Cost from the usage of every model call and the per-million rates in AI:Pricing.
        var costs = provider.GetRequiredService<CostMonitoringService>();
        var today = await costs.GetDailyCostReportAsync(DateOnly.FromDateTime(DateTime.UtcNow));
        Console.WriteLine($"Today: {today.CallCount} model calls, {today.InputTokens} input ({today.CachedInputTokens} cached) and " +
            $"{today.OutputTokens} output tokens, {today.EmbeddingTokens} embedding tokens, " +
            $"{Usd(today.EstimatedCostUsd)} at the placeholder rates; alert {today.AlertTriggered}");
        var conversation = await provider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
        var perConversation = await costs.GetConversationCostAsync(conversation!.Id);
        Console.WriteLine($"Conversation: {perConversation.RunCount} agent runs, {perConversation.TotalTokens} tokens, " +
            $"over the token limit {perConversation.OverTokenLimit}");

        var strict = new CostMonitoringService(provider.GetRequiredService<CopilotDbContext>(), provider.GetRequiredService<ITelemetryCollector>(),
            Options.Create(new BudgetOptions { DailyLimitUsd = 0.001m }), provider.GetRequiredService<IOptions<TokenPricingOptions>>());
        var overBudget = await strict.GetDailyCostReportAsync(DateOnly.FromDateTime(DateTime.UtcNow));
        Console.WriteLine($"With a daily limit of {Usd(0.001m)}: alert {overBudget.AlertTriggered}, exceeded {overBudget.BudgetExceeded}");
        passed &= today.CallCount > 0 && today.EmbeddingTokens > 0 && today.EstimatedCostUsd > 0 && !today.AlertTriggered
            && overBudget.BudgetExceeded;

        // 4. The embedding cache answers a repeated request without spending tokens.
        var embeddings = provider.GetRequiredService<IEmbeddingClient>();
        var first = await embeddings.GenerateAsync("How do I unlock an account after a password reset?");
        var repeated = await embeddings.GenerateAsync("How do I unlock an account after a password reset?");
        Console.WriteLine($"Embedding client: {embeddings.GetType().Name}; tokens {first.TokenCount}, then {repeated.TokenCount} from the cache");
        passed &= embeddings is CachingEmbeddingClient && first.TokenCount > 0 && repeated.TokenCount == 0;

        // 5. End-to-end latency over a few iterations.
        var benchmark = provider.GetRequiredService<WorkflowBenchmark>();
        for (var i = 0; i < 5; i++)
        {
            await benchmark.RunIterationAsync(SampleData.PasswordTicketId, "engineer-ana", $"Benchmark {i + 1}");
        }

        var stats = benchmark.GetStatistics();
        Console.WriteLine($"Benchmark: {stats.Count} runs, P50 {stats.P50} ms, P95 {stats.P95} ms, P99 {stats.P99} ms");
        passed &= stats.Count == 5 && stats.P50 <= stats.P95 && stats.P95 <= stats.P99;

        tracing?.ForceFlush();
        metrics?.ForceFlush();
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    private static string Usd(decimal value) => value.ToString("$0.000000", CultureInfo.InvariantCulture);
}
