using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public class TriageAgent : IAgent
{
    public string Name { get; }
    public TriageAgent(string name) => Name = name;
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Chapter 10 implements the triage agent.");
}
