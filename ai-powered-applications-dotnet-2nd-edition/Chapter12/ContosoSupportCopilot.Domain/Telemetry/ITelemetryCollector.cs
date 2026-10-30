using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Telemetry;

// Domain events that the GenAI instrumentation cannot see: run records, workflow steps,
// cache hits and budget decisions. Implementations record them as spans and metrics.
public interface ITelemetryCollector
{
    void TrackAgentRun(AgentRun run);
    void TrackWorkflowStep(string stepName, Guid workflowId, long durationMs);
    void TrackEmbeddingCache(bool hit, int tokensSaved);
    void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties);
}
