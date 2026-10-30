namespace WarehouseApi.Contracts;

// The correlation ID is not part of the request; the service reads it from ICorrelationContext
public record ReservationRequest(
    Guid OrderId,
    string WarehouseCode);
