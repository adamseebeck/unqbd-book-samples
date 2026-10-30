namespace ContosoSupportCopilot.Domain.Models;

public record AgentContext
{
    public Guid ConversationId { get; init; }
    public SupportTicket? CurrentTicket { get; init; }
    public string Intent { get; init; } = string.Empty;

    // The support engineer the agent acts for. Retrieval authorizes every article for this ID.
    public string RequesterId { get; init; } = string.Empty;

    // What the knowledge agent found, for the drafting agent; the code that hands the work over sets it.
    public DraftingKnowledge? Knowledge { get; init; }

    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record AgentResult
{
    public string Response { get; init; } = string.Empty;
    public bool RequiresHandoff { get; init; }
    public string? NextAgent { get; init; }
    public Dictionary<string, object> Artifacts { get; init; } = new();
}

// One name per agent, shared by the agents, the coordinator and the services that call them.
public static class AgentNames
{
    public const string Triage = "TriageAgent";
    public const string Knowledge = "KnowledgeAgent";
    public const string Drafting = "DraftingAgent";
    public const string Escalation = "EscalationAgent";
}

// The artifact keys that agents write and later steps read.
public static class AgentArtifactKeys
{
    public const string FailureMode = "failure_mode";
    public const string Intent = "intent";
    public const string Urgency = "urgency";
    public const string ProductArea = "product_area";
    public const string Confidence = "confidence";
    public const string PriorityScore = "priority_score";
    public const string SearchQuery = "search_query";
    public const string Citations = "citations";
    public const string IsGrounded = "is_grounded";
    public const string Draft = "draft";
    public const string IsFallback = "is_fallback";
    public const string ModelId = "model_id";
    public const string InputTokens = "input_tokens";
    public const string OutputTokens = "output_tokens";
    public const string EmbeddingTokens = "embedding_tokens";
    public const string EscalationDraft = "escalation_draft";
    public const string GeneratedAt = "generated_at";
}
