using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Telemetry;

namespace ContosoSupportCopilot.Infrastructure.Telemetry;

// A decorator: every run record that an agent stores also becomes a span and metrics.
public sealed class TelemetryAgentRunRepository : IAgentRunRepository
{
    private readonly IAgentRunRepository _inner;
    private readonly ITelemetryCollector _telemetry;

    public TelemetryAgentRunRepository(IAgentRunRepository inner, ITelemetryCollector telemetry)
    {
        _inner = inner;
        _telemetry = telemetry;
    }

    public async Task LogAsync(AgentRun run, CancellationToken cancellationToken = default)
    {
        await _inner.LogAsync(run, cancellationToken);
        _telemetry.TrackAgentRun(run);
    }

    public Task<IReadOnlyList<AgentRun>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
        _inner.GetByConversationAsync(conversationId, cancellationToken);
}
