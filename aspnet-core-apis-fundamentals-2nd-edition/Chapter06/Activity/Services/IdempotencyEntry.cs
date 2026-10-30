namespace WarehouseApi.Services;

public record IdempotencyEntry(
    object Response,
    int StatusCode,
    string RequestHash,
    DateTime Timestamp);
