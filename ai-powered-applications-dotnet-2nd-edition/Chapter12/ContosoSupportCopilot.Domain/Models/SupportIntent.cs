namespace ContosoSupportCopilot.Domain.Models;

public record SupportIntent
{
    public string Intent { get; init; } = string.Empty;
    public string Urgency { get; init; } = string.Empty;
    public string ProductArea { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
}
