using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Fulfillment;

public class FulfillmentOptions
{
    public const string SectionName = "Fulfillment";

    // Attempts per job; each attempt is one ERP call with the client's own retries
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    // The wait before the second attempt; it doubles for every further attempt
    [Range(1, 300)]
    public int RetryDelaySeconds { get; set; } = 5;

    // How often the processor sends the Pending shipments that no queued job carries
    [Range(1, 3600)]
    public int SweepIntervalSeconds { get; set; } = 60;

    // The queue holds 1000 jobs; from this depth on, the readiness report shows Degraded
    [Range(1, 1000)]
    public int DegradedQueueDepth { get; set; } = 800;

    // False in a deployment slot that shares the production database: only production calls the ERP
    public bool ProcessorEnabled { get; set; } = true;
}
