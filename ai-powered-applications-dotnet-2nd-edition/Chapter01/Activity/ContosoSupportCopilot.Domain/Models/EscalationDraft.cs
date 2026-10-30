namespace ContosoSupportCopilot.Domain.Models;

public record EscalationDraft
{
    public Guid TicketId { get; init; }
    public string EngineeringSummary { get; init; } = string.Empty;
    public string ReproductionSteps { get; init; } = string.Empty;
    public string BusinessImpact { get; init; } = string.Empty;
}
