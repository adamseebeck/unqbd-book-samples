namespace WarehouseApi.Contracts;

public record HealthCheckResponse(
    string Status,
    DateTime Timestamp,
    string Version
);
