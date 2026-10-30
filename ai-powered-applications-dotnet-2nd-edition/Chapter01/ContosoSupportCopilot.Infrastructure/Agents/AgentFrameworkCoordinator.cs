using System.Collections.Concurrent;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public class AgentFrameworkCoordinator : IAgentCoordinator
{
    // One instance serves every request, so a registration can run while other requests look agents up.
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    public Task RegisterAgentAsync(string agentName, IAgent agent, CancellationToken cancellationToken = default)
    {
        _agents[agentName] = agent;
        return Task.CompletedTask;
    }

    public Task<AgentResult> ExecuteAgentAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentName, out var agent))
            throw new ArgumentException($"Agent {agentName} not registered", nameof(agentName));
        return agent.ExecuteAsync(context, cancellationToken);
    }

    public Task HandoffAsync(string fromAgent, string toAgent, AgentContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Chapter 2 implements agent handoff.");
}
