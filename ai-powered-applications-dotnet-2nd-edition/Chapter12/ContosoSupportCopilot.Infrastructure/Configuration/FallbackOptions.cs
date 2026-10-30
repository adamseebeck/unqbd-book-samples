namespace ContosoSupportCopilot.Infrastructure.Configuration;

public record FallbackOptions
{
    public const string SectionName = "Fallback";

    // The engineer sends the reply to the customer, so the default speaks to the customer.
    public string FallbackMessage { get; init; } = "Thank you for your patience. A support engineer is reviewing your ticket and will reply shortly.";
    public bool EnableCache { get; init; } = true;
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}
