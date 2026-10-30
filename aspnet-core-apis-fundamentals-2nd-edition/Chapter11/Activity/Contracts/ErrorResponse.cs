namespace WarehouseApi.Contracts;

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string CorrelationId
);
