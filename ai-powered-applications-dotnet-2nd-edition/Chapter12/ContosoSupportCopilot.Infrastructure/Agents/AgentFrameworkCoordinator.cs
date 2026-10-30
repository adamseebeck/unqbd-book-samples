using System.Collections.Concurrent;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Scoped: agents come from the caller's scope as keyed services, so they share its DbContext and
// never outlive the request. RegisterAgentAsync adds an agent for this scope only, such as a test double.
public class AgentFrameworkCoordinator : IAgentCoordinator
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AgentFrameworkCoordinator> _logger;
    // A ConcurrentDictionary, as in Chapter 1: a registration never races a lookup.
    private readonly ConcurrentDictionary<string, IAgent> _registered = new(StringComparer.Ordinal);

    public AgentFrameworkCoordinator(IServiceProvider services, ILogger<AgentFrameworkCoordinator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task RegisterAgentAsync(string agentName, IAgent agent, CancellationToken cancellationToken = default)
    {
        _registered[agentName] = agent;
        return Task.CompletedTask;
    }

    public async Task<AgentResult> ExecuteAgentAsync(string agentName, AgentContext context, CancellationToken cancellationToken = default)
    {
        var agent = _registered.GetValueOrDefault(agentName)
            ?? _services.GetKeyedService<IAgent>(agentName)
            ?? throw new ArgumentException($"Agent '{agentName}' is not registered", nameof(agentName));

        return await agent.ExecuteAsync(context, cancellationToken);
    }

    // A handoff runs the next agent and tells it who handed the work over.
    public async Task<AgentResult> HandoffAsync(string fromAgent, string toAgent, AgentContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handoff from {FromAgent} to {ToAgent} for conversation {ConversationId}",
            fromAgent, toAgent, context.ConversationId);

        var handoff = context with { Metadata = new Dictionary<string, object>(context.Metadata) { ["handoff_from"] = fromAgent } };
        return await ExecuteAgentAsync(toAgent, handoff, cancellationToken);
    }
}
