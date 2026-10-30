namespace ContosoSupportCopilot.Domain.Models;

public record AgentContext
{
    public Guid ConversationId { get; init; }
    public SupportTicket? CurrentTicket { get; init; }
    public string Intent { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record AgentResult
{
    public string Response { get; init; } = string.Empty;
    public bool RequiresHandoff { get; init; }
    public string? NextAgent { get; init; }
    public Dictionary<string, object> Artifacts { get; init; } = new();
}
