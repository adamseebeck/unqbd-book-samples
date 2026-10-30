namespace ContosoSupportCopilot.Domain.Models;

// One row per conversation, agent and engineer: the agent's session as the framework serialized it.
// The engineer is part of the key because the session holds what that engineer's tools returned.
public record AgentSessionRecord
{
    public Guid ConversationId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string RequesterId { get; init; } = string.Empty;
    public string SessionJson { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }

    // How many times the session was saved: a concurrency token, as CurrentTurn is for conversations.
    // A save from an older version fails, so one run never overwrites the turn that another run stored.
    public int Version { get; init; }
}
