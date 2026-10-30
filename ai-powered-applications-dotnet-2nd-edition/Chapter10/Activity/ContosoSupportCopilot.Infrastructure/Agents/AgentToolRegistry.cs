using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Scoped like the tools it holds: a singleton registry would keep the first request's tools,
// and their DbContext, forever.
public class AgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.Ordinal);

    public AgentToolRegistry(IEnumerable<IAgentTool> tools)
    {
        foreach (var tool in tools)
        {
            Register(tool);
        }
    }

    public void Register(IAgentTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Name] = tool;
    }

    public IAgentTool Resolve(string name) =>
        _tools.TryGetValue(name, out var tool)
            ? tool
            : throw new ArgumentException($"Tool '{name}' is not registered.", nameof(name));

    public bool TryResolve(string name, out IAgentTool? tool) => _tools.TryGetValue(name, out tool);
}
