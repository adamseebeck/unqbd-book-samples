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

    public async Task<AgentResult> ExecuteAgentAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentName, out var agent))
        {
            throw new ArgumentException($"Agent '{agentName}' is not registered", nameof(agentName));
        }

        return await agent.ExecuteAsync(context, cancellationToken);
    }

    public async Task HandoffAsync(string fromAgent, string toAgent, AgentContext context, CancellationToken cancellationToken = default)
    {
        // Handoff tracking (who handed off to whom) is added with the agents in Chapter 10.
        await ExecuteAgentAsync(toAgent, context, cancellationToken);
    }
}
