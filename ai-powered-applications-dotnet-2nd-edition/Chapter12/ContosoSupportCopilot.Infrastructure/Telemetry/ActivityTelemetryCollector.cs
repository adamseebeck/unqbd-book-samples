using System.Diagnostics;
using System.Diagnostics.Metrics;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Telemetry;

namespace ContosoSupportCopilot.Infrastructure.Telemetry;

// Spans and metrics through System.Diagnostics. The OpenTelemetry SDK that Program.cs registers
// collects them with the GenAI spans; without a listener, they cost almost nothing.
public sealed class ActivityTelemetryCollector : ITelemetryCollector
{
    public const string SourceName = "ContosoSupportCopilot";

    private static readonly ActivitySource Source = new(SourceName);

    private readonly Counter<long> _agentRuns;
    private readonly Histogram<long> _agentTokens;
    private readonly Histogram<long> _stepDuration;
    private readonly Counter<long> _cacheLookups;
    private readonly Counter<long> _cacheTokensSaved;

    // The host's meter factory owns the meter, so each service provider gets its own and disposes it.
    public ActivityTelemetryCollector(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(SourceName);
        _agentRuns = meter.CreateCounter<long>("contoso.agent.runs", description: "Agent runs by agent and outcome");
        _agentTokens = meter.CreateHistogram<long>("contoso.agent.tokens", "{token}", "Tokens per agent run");
        _stepDuration = meter.CreateHistogram<long>("contoso.workflow.step.duration", "ms", "Workflow step duration");
        _cacheLookups = meter.CreateCounter<long>("contoso.embedding.cache.lookups", description: "Embedding cache hits and misses");
        _cacheTokensSaved = meter.CreateCounter<long>("contoso.embedding.cache.tokens_saved", "{token}", "Embedding tokens that cache hits did not spend");
    }

    public void TrackAgentRun(AgentRun run)
    {
        // The run's tokens are already under gen_ai.usage.*, on its chat spans and again on the invoke_agent
        // span. Here they get contoso.* names, so this span adds no third copy; total tokens from the chat and
        // embeddings spans only.
        using var activity = Source.StartActivity($"agent_run {run.AgentName}");
        activity?.SetTag("gen_ai.agent.name", run.AgentName);
        activity?.SetTag("contoso.agent.model", run.ModelId);
        activity?.SetTag("contoso.agent.input_tokens", run.InputTokens);
        activity?.SetTag("contoso.agent.output_tokens", run.OutputTokens);
        activity?.SetTag("contoso.conversation.id", run.ConversationId);
        activity?.SetTag("contoso.correlation.id", run.CorrelationId);
        activity?.SetTag("contoso.fallback", run.IsFallback);

        _agentRuns.Add(1, new("gen_ai.agent.name", run.AgentName), new("contoso.fallback", run.IsFallback));
        _agentTokens.Record(run.InputTokens, new("gen_ai.agent.name", run.AgentName), new("gen_ai.token.type", "input"));
        _agentTokens.Record(run.OutputTokens, new("gen_ai.agent.name", run.AgentName), new("gen_ai.token.type", "output"));
    }

    public void TrackWorkflowStep(string stepName, Guid workflowId, long durationMs)
    {
        // The step has already ended, so the span starts when the step started and ends now. Its agent and
        // chat spans ran before it existed, so a trace shows them beside this span rather than below it.
        using var activity = Source.StartActivity($"workflow_step {stepName}");
        activity?.SetStartTime(DateTime.UtcNow.AddMilliseconds(-durationMs));
        activity?.SetTag("contoso.workflow.id", workflowId);
        activity?.SetTag("contoso.workflow.step", stepName);
        _stepDuration.Record(durationMs, new KeyValuePair<string, object?>("contoso.workflow.step", stepName));
    }

    public void TrackEmbeddingCache(bool hit, int tokensSaved)
    {
        _cacheLookups.Add(1, new KeyValuePair<string, object?>("contoso.cache.hit", hit));
        if (tokensSaved > 0)
        {
            _cacheTokensSaved.Add(tokensSaved);
        }
    }

    public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
    {
        using var activity = Source.StartActivity(eventName);
        foreach (var (key, value) in properties)
        {
            activity?.SetTag(key, value);
        }
    }
}
