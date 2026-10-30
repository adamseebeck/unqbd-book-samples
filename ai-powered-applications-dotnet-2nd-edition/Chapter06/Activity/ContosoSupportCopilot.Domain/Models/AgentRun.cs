namespace ContosoSupportCopilot.Domain.Models;

public record AgentRun
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid TicketId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int LatencyMs { get; init; }
    public string ModelId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsFallback { get; init; }
    public string? ErrorMessage { get; init; }
}
