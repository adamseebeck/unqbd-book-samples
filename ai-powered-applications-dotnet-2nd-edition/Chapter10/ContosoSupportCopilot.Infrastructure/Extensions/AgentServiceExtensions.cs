using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class AgentServiceExtensions
{
    // Call after the services the tools wrap: intent extraction, Semantic Kernel, drafting and RAG.
    public static IServiceCollection AddAgentServices(this IServiceCollection services)
    {
        // A singleton: the factory holds the shared chat client and one agent per name, and no request state.
        services.AddSingleton<SupportAgentFactory>();
        services.AddScoped<IAgentSessionRepository, SqlAgentSessionRepository>();
        services.AddScoped<AgentRunner>();

        // Tools wrap scoped services, so the tools and their registry are scoped too.
        services.AddScoped<IAgentTool, TicketClassificationTool>();
        services.AddScoped<IAgentTool, PriorityScoreTool>();
        services.AddScoped<IAgentTool, KnowledgeRetrievalTool>();
        services.AddScoped<IAgentTool, DraftingTool>();
        services.AddScoped<AgentToolRegistry>();

        // Offline, a draft that receives a knowledge answer restates it, so runs without a model show its facts.
        services.AddSingleton<IOfflineResponder, OfflineGroundedDraftResponder>();

        // Agents are keyed by name; the coordinator resolves them from the caller's scope.
        services.AddKeyedScoped<IAgent, TriageAgent>(AgentNames.Triage);
        services.AddKeyedScoped<IAgent, KnowledgeAgent>(AgentNames.Knowledge);
        services.AddKeyedScoped<IAgent, DraftingAgent>(AgentNames.Drafting);
        services.AddScoped<IAgentCoordinator, AgentFrameworkCoordinator>();

        return services;
    }
}
