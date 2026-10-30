using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IAgentCoordinator
{
    Task<AgentResult> ExecuteAgentAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default);
    Task HandoffAsync(string fromAgent, string toAgent, AgentContext context, CancellationToken cancellationToken = default);
    Task RegisterAgentAsync(string agentName, IAgent agent, CancellationToken cancellationToken = default);
}

public interface IAgent
{
    string Name { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
