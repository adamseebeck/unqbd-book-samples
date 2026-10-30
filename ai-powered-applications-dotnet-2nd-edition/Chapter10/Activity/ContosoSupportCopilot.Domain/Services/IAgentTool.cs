using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IAgentTool
{
    // The function name the model sees, such as search_knowledge_base.
    string Name { get; }
    string Description { get; }

    // The arguments the model supplies. Everything else comes from AgentToolContext.
    Type InputType { get; }
    Type OutputType { get; }

    Task<AgentToolResult> ExecuteAsync(object input, AgentToolContext context, CancellationToken cancellationToken = default);
}
