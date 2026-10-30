namespace ContosoSupportCopilot.Infrastructure.Configuration;

// Prices in US dollars per one million tokens for the chat deployment.
// Copy the current rates for your model and deployment type from the pricing page.
public class TokenPricingOptions
{
    public const string SectionName = "AI:Pricing";

    public decimal InputPerMillion { get; set; }
    public decimal CachedInputPerMillion { get; set; }
    public decimal OutputPerMillion { get; set; }

    // Embedding deployments charge for input tokens only, at their own per-million rate.
    public decimal EmbeddingPerMillion { get; set; }
}
