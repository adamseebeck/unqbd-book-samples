using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Builds the Agent Framework agents on the application's IChatClient, so every agent uses the configured
// provider, the "kernel" resilience pipeline from Chapter 4 and the logging. The pipeline wraps each model
// call of a run on its own, so a retry never repeats the tools that earlier calls of the run executed.
public sealed class SupportAgentFactory : IDisposable
{
    public const string TelemetrySourceName = "ContosoSupportCopilot.Agents";

    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, OpenTelemetryAgent> _agents = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private readonly bool _enableSensitiveData;

    public SupportAgentFactory(IChatClient chatClient, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _chatClient = chatClient;
        _loggerFactory = loggerFactory;

        // From Chapter 12: the same switch as the chat client's spans, off unless Telemetry:EnableSensitiveData is true.
        _enableSensitiveData = configuration.GetValue("Telemetry:EnableSensitiveData", false);
    }

    // One agent per name for the life of the application: its telemetry wrapper owns an ActivitySource and a
    // Meter, which belong to the process, not to a request. Every run passes its own tools in its run options,
    // and callers of one name pass the same instructions.
    public AIAgent GetAgent(string name, string instructions)
    {
        lock (_lock)
        {
            if (!_agents.TryGetValue(name, out var agent))
            {
                agent = Create(name, instructions, []);
                _agents[name] = agent;
            }

            return agent;
        }
    }

    // An agent with its tools built in, for code that keeps one for a single job, such as a workflow.
    // The caller disposes it.
    public OpenTelemetryAgent Create(string name, string instructions, IList<AITool> tools)
    {
        // No temperature: reasoning deployments reject it. Instructions and tools live in ChatOptions.
        var agent = new ChatClientAgent(
            _chatClient,
            new ChatClientAgentOptions
            {
                // A stable ID: telemetry and workflow checkpoints identify the agent by it.
                Id = name,
                Name = name,
                ChatOptions = new ChatOptions { Instructions = instructions, Tools = tools }
            },
            _loggerFactory);

        // invoke_agent, chat and execute_tool spans with the GenAI semantic conventions. Prompts, answers and
        // tool arguments enter the spans only when EnableSensitiveData is true.
        return new OpenTelemetryAgent(agent, TelemetrySourceName) { EnableSensitiveData = _enableSensitiveData };
    }

    // The container disposes this singleton at shutdown; disposing an agent leaves the shared chat client open.
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var agent in _agents.Values)
            {
                agent.Dispose();
            }

            _agents.Clear();
        }
    }
}
