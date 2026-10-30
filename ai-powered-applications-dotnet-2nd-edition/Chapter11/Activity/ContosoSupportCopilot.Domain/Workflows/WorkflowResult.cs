using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Workflows;

public record WorkflowResult
{
    public bool IsSuccess { get; init; }
    public string FinalResponse { get; init; } = string.Empty;
    public IReadOnlyList<CitationReference> Citations { get; init; } = Array.Empty<CitationReference>();
    public bool EscalationRequired { get; init; }
    public string? ErrorMessage { get; init; }
    public WorkflowStep FinalStep { get; init; }

    // Set when a transient failure stopped the run at FinalStep: resume the workflow after this time.
    public DateTime? RetryAfter { get; init; }

    public Guid WorkflowId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
