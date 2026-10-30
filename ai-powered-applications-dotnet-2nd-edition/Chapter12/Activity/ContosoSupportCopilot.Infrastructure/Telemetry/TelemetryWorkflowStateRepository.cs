using System.Diagnostics;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Domain.Workflows;

namespace ContosoSupportCopilot.Infrastructure.Telemetry;

// A decorator: the workflow saves its state when a step ends, so each save records the step that ran since
// the run's previous save or load, with its duration. The workflow itself does not change.
public sealed class TelemetryWorkflowStateRepository : IWorkflowStateRepository
{
    private readonly IWorkflowStateRepository _inner;
    private readonly ITelemetryCollector _telemetry;
    private readonly Dictionary<Guid, (WorkflowStep Step, long Timestamp)> _current = new();

    public TelemetryWorkflowStateRepository(IWorkflowStateRepository inner, ITelemetryCollector telemetry)
    {
        _inner = inner;
        _telemetry = telemetry;
    }

    public async Task<WorkflowState?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var state = await _inner.GetAsync(workflowId, cancellationToken);
        if (state is not null)
        {
            // A resumed run starts timing its current step here.
            _current[workflowId] = (state.CurrentStep, Stopwatch.GetTimestamp());
        }

        return state;
    }

    public async Task<WorkflowState> SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        var saved = await _inner.SaveAsync(state, cancellationToken);
        var now = Stopwatch.GetTimestamp();
        if (_current.TryGetValue(saved.WorkflowId, out var running))
        {
            _telemetry.TrackWorkflowStep(running.Step.ToString(), saved.WorkflowId, (long)Stopwatch.GetElapsedTime(running.Timestamp, now).TotalMilliseconds);
        }

        _current[saved.WorkflowId] = (saved.CurrentStep, now);
        return saved;
    }
}
