using System.ComponentModel.DataAnnotations;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Monitoring;

public class BudgetOptions
{
    public const string SectionName = "Budget";

    [Range(0.001, 1_000_000, ErrorMessage = "Budget:DailyLimitUsd must be between 0.001 and 1000000")]
    public decimal DailyLimitUsd { get; set; } = 100m;

    [Range(1, 100, ErrorMessage = "Budget:AlertThresholdPercent must be between 1 and 100")]
    public decimal AlertThresholdPercent { get; set; } = 80m;

    // A conversation above this many tokens is flagged: a loop or an oversized prompt, usually.
    [Range(1_000, 10_000_000, ErrorMessage = "Budget:MaxTokensPerConversation must be between 1000 and 10000000")]
    public long MaxTokensPerConversation { get; set; } = 50_000;
}

public record CostReport(DateOnly Date, int CallCount, long InputTokens, long CachedInputTokens, long OutputTokens,
    long EmbeddingTokens, decimal EstimatedCostUsd, bool AlertTriggered, bool BudgetExceeded);

public record ConversationCostReport(Guid ConversationId, int RunCount, long TotalTokens, decimal EstimatedCostUsd,
    bool OverTokenLimit);

// Prices token usage with the per-million rates in AI:Pricing (Chapter 3, and Chapter 8 for embeddings).
public class CostMonitoringService
{
    private readonly CopilotDbContext _dbContext;
    private readonly ITelemetryCollector _telemetry;
    private readonly BudgetOptions _budget;
    private readonly TokenPricingOptions _pricing;

    public CostMonitoringService(
        CopilotDbContext dbContext,
        ITelemetryCollector telemetry,
        IOptions<BudgetOptions> budget,
        IOptions<TokenPricingOptions> pricing)
    {
        _dbContext = dbContext;
        _telemetry = telemetry;
        _budget = budget.Value;
        _pricing = pricing.Value;
    }

    // The day's spend from the ModelUsage rows: every call of the registered chat client and embedding
    // generator, with cached input at its own rate. The Chapter 9 judges use a client of their own and
    // are not included; price an evaluation run separately.
    public async Task<CostReport> GetDailyCostReportAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        // The database adds the tokens up per operation; the Timestamp index serves the range.
        var totals = await _dbContext.ModelUsage
            .Where(u => u.Timestamp >= start && u.Timestamp < end)
            .GroupBy(u => u.Operation)
            .Select(g => new
            {
                Operation = g.Key,
                Calls = g.Count(),
                Input = g.Sum(u => u.InputTokens),
                Cached = g.Sum(u => u.CachedInputTokens),
                Output = g.Sum(u => u.OutputTokens)
            })
            .ToListAsync(cancellationToken);

        var chat = totals.FirstOrDefault(t => t.Operation == ModelUsageRecord.Chat);
        var embeddingTokens = totals.FirstOrDefault(t => t.Operation == ModelUsageRecord.Embeddings)?.Input ?? 0;
        var cost = TokenCostCalculator.Calculate(new UsageDetails
            {
                InputTokenCount = chat?.Input ?? 0,
                CachedInputTokenCount = chat?.Cached ?? 0,
                OutputTokenCount = chat?.Output ?? 0
            }, _pricing)
            + TokenCostCalculator.CalculateEmbedding(embeddingTokens, _pricing);

        var report = new CostReport(date, totals.Sum(t => t.Calls), chat?.Input ?? 0, chat?.Cached ?? 0, chat?.Output ?? 0,
            embeddingTokens, cost,
            AlertTriggered: cost >= _budget.DailyLimitUsd * _budget.AlertThresholdPercent / 100m,
            BudgetExceeded: cost >= _budget.DailyLimitUsd);

        if (report.AlertTriggered)
        {
            _telemetry.TrackEvent("budget_alert", new Dictionary<string, object?>
            {
                ["contoso.budget.date"] = date.ToString("yyyy-MM-dd"),
                ["contoso.budget.cost_usd"] = (double)cost,
                ["contoso.budget.limit_usd"] = (double)_budget.DailyLimitUsd,
                ["contoso.budget.exceeded"] = report.BudgetExceeded
            });
        }

        return report;
    }

    // One conversation from its AgentRun records: each agent's own model calls, plus the grounded answer
    // that the knowledge agent adds. Calls inside other tools (intent extraction in classify_ticket),
    // summaries and embeddings carry no conversation ID, so only the daily report sees them. AgentRun keeps
    // input and output totals, so cached input is priced at the full input rate here.
    public async Task<ConversationCostReport> GetConversationCostAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var runs = await _dbContext.AgentRuns
            .Where(r => r.ConversationId == conversationId)
            .Select(r => new { r.InputTokens, r.OutputTokens })
            .ToListAsync(cancellationToken);

        long input = runs.Sum(r => (long)r.InputTokens);
        long output = runs.Sum(r => (long)r.OutputTokens);
        var cost = TokenCostCalculator.Calculate(new UsageDetails { InputTokenCount = input, OutputTokenCount = output }, _pricing);
        return new ConversationCostReport(conversationId, runs.Count, input + output, cost,
            OverTokenLimit: input + output > _budget.MaxTokensPerConversation);
    }
}
