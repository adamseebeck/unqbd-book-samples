namespace ContosoSupportCopilot.Domain.Models;

public record EscalationDraft(
    string Summary,
    string Impact,
    string ReproductionSteps,
    string RecommendedPriority);
