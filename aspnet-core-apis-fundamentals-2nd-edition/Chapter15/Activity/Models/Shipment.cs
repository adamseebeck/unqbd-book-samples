using WarehouseApi.Contracts;

namespace WarehouseApi.Models;

// The ERP shipment of an allocated order. The reservation adds the row in its own transaction,
// so a shipment request that was not sent before a restart is still on record
public class Shipment
{
    public const int MaxErrorLength = 500;

    public Guid OrderId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? ShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    // The request that allocated the order; the background job logs it with every entry
    public string CorrelationId { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }
    public DateTime? RegisteredAt { get; set; }

    // Dead-letter queue: when the job last failed, how often it was retried, and when the next retry is due
    public DateTime? DeadLetteredAt { get; set; }
    public int DeadLetterRetries { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LastRetriedAt { get; set; }
}
