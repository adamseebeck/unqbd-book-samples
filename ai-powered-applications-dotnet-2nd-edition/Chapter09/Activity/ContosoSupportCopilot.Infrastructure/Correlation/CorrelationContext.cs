using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Correlation;

public class CorrelationContext : ICorrelationContext
{
    // The value travels with the async flow, not with this object, so one instance serves every request.
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    // Without a value (outside a request, for example), the first read creates one for the current flow.
    public string CorrelationId => CurrentCorrelationId.Value ??= Guid.NewGuid().ToString();

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));
        }

        CurrentCorrelationId.Value = correlationId;
    }
}
