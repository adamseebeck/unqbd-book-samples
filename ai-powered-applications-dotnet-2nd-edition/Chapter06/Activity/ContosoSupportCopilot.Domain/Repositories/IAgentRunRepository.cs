using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Repositories;

public interface IAgentRunRepository
{
    Task LogAsync(AgentRun run, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentRun>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
