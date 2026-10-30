namespace WarehouseApi.Fulfillment;

// Identifiers only: the job reads the current order and shipment from the database when it runs
public record FulfillmentJob(
    Guid OrderId,
    string CorrelationId,
    DateTimeOffset EnqueuedAt);
