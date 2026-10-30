namespace WarehouseApi.Contracts;

public record WarehouseResponse(
    int Id,
    string Code,
    string Name,
    string Location,
    bool IsOperational);
