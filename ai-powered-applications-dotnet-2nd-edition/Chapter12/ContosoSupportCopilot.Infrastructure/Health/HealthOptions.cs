using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Health;

public class HealthOptions
{
    public const string SectionName = "Health";

    // How often the background monitor sends its one short model request: while the provider answers,
    // and while it does not. Readiness only reads the latest result.
    [Range(30, 86_400, ErrorMessage = "Health:AIProbeIntervalSeconds must be between 30 and 86400")]
    public int AIProbeIntervalSeconds { get; set; } = 300;

    [Range(5, 3_600, ErrorMessage = "Health:AIProbeRetrySeconds must be between 5 and 3600")]
    public int AIProbeRetrySeconds { get; set; } = 30;

    // How long one probe may take; a provider that answers more slowly is reported as Unhealthy.
    [Range(1, 120, ErrorMessage = "Health:AIProbeTimeoutSeconds must be between 1 and 120")]
    public int AIProbeTimeoutSeconds { get; set; } = 10;
}
