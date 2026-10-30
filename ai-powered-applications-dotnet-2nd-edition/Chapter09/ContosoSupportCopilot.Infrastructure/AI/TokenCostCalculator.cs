using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI;

public static class TokenCostCalculator
{
    public static decimal Calculate(UsageDetails? usage, TokenPricingOptions pricing)
    {
        if (usage is null)
        {
            return 0m;
        }

        // Cached input tokens are part of InputTokenCount and bill at the cached rate.
        // Reasoning tokens are part of OutputTokenCount and bill at the output rate.
        var input = usage.InputTokenCount ?? 0;
        var cached = Math.Min(usage.CachedInputTokenCount ?? 0, input);
        var output = usage.OutputTokenCount ?? 0;

        var cost = ((input - cached) * pricing.InputPerMillion)
            + (cached * pricing.CachedInputPerMillion)
            + (output * pricing.OutputPerMillion);

        return cost / 1_000_000m;
    }

    public static decimal CalculateEmbedding(long inputTokens, TokenPricingOptions pricing) =>
        inputTokens * pricing.EmbeddingPerMillion / 1_000_000m;
}
