namespace ContosoSupportCopilot.Infrastructure.Monitoring;

// One row per model call that reported its usage: every call of the registered chat client and embedding
// generator, including those that no AgentRun records, such as intent extraction inside a tool or a summary.
public class ModelUsageRecord
{
    public const string Chat = "chat";
    public const string Embeddings = "embeddings";

    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long CachedInputTokens { get; set; }
    public long OutputTokens { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
