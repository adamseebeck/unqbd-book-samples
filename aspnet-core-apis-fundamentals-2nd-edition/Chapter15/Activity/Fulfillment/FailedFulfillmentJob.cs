namespace WarehouseApi.Fulfillment;

// A shipment request that failed every attempt
public record FailedFulfillmentJob(
    Guid OrderId,
    string CorrelationId,
    string Error,
    DateTimeOffset FailedAt);
