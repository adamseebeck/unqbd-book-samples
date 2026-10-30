using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Repositories;

// Stores each agent's serialized session next to the conversation it belongs to.
public interface IAgentSessionRepository
{
    // The stored session with its version, or null before the agent's first turn.
    Task<AgentSessionRecord?> GetAsync(Guid conversationId, string agentName, string requesterId, CancellationToken cancellationToken = default);

    // Saves the session while the stored row still has the record's Version (0: no row yet) and returns the
    // new version. Throws AgentSessionConflictException when another run saved the session first.
    Task<int> SaveAsync(AgentSessionRecord session, CancellationToken cancellationToken = default);
}
