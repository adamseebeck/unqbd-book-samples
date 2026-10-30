using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public record KernelResilienceOptions : IValidatableObject
{
    public const string SectionName = "KernelResilience";

    [Range(1, 5, ErrorMessage = "KernelResilience:MaxRetryAttempts must be between 1 and 5")]
    public int MaxRetryAttempts { get; init; } = 2;

    [Range(1, 60_000, ErrorMessage = "KernelResilience:BaseDelayMilliseconds must be between 1 and 60000")]
    public int BaseDelayMilliseconds { get; init; } = 1000;

    [Range(1, 60_000, ErrorMessage = "KernelResilience:MaxDelayMilliseconds must be between 1 and 60000")]
    public int MaxDelayMilliseconds { get; init; } = 10_000;

    [Range(1, 300, ErrorMessage = "KernelResilience:AttemptTimeoutSeconds must be between 1 and 300")]
    public int AttemptTimeoutSeconds { get; init; } = 45;

    [Range(1, 600, ErrorMessage = "KernelResilience:TotalTimeoutSeconds must be between 1 and 600")]
    public int TotalTimeoutSeconds { get; init; } = 120;

    [Range(0.01, 1.0, ErrorMessage = "KernelResilience:CircuitBreakerFailureRatio must be between 0.01 and 1.0")]
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;

    [Range(2, 1000, ErrorMessage = "KernelResilience:CircuitBreakerMinimumThroughput must be between 2 and 1000")]
    public int CircuitBreakerMinimumThroughput { get; init; } = 5;

    [Range(1, 3600, ErrorMessage = "KernelResilience:CircuitBreakerSamplingSeconds must be between 1 and 3600")]
    public int CircuitBreakerSamplingSeconds { get; init; } = 60;

    [Range(1, 3600, ErrorMessage = "KernelResilience:CircuitBreakerBreakSeconds must be between 1 and 3600")]
    public int CircuitBreakerBreakSeconds { get; init; } = 60;

    // Settings that are valid alone can still contradict each other.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AttemptTimeoutSeconds > TotalTimeoutSeconds)
        {
            yield return new ValidationResult(
                "KernelResilience:AttemptTimeoutSeconds must not be greater than KernelResilience:TotalTimeoutSeconds",
                [nameof(AttemptTimeoutSeconds), nameof(TotalTimeoutSeconds)]);
        }

        if (BaseDelayMilliseconds > MaxDelayMilliseconds)
        {
            yield return new ValidationResult(
                "KernelResilience:BaseDelayMilliseconds must not be greater than KernelResilience:MaxDelayMilliseconds",
                [nameof(BaseDelayMilliseconds), nameof(MaxDelayMilliseconds)]);
        }
    }
}
