using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Fulfillment;

public class DeadLetterOptions
{
    public const string SectionName = "Fulfillment:DeadLetter";

    // How often the retry service looks for failed jobs
    [Range(1, 86_400)]
    public int RetryIntervalSeconds { get; set; } = 300;

    // The wait before the first retry of a failed job; it doubles after every failed retry
    [Range(1, 86_400)]
    public int BaseRetryDelaySeconds { get; set; } = 60;

    // After this many failed retries a job stays in the queue for an operator
    [Range(1, 20)]
    public int MaxRetries { get; set; } = 5;

    [Range(1, 1_000)]
    public int BatchSize { get; set; } = 20;

    [Range(0, 100_000)]
    public int DegradedThreshold { get; set; } = 50;

    [Range(0, 100_000)]
    public int UnhealthyThreshold { get; set; } = 100;
}
