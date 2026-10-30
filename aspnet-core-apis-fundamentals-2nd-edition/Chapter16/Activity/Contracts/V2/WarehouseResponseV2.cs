namespace WarehouseApi.Contracts.V2;

// V2 identifies a warehouse by its code, as the stock and order routes do; V1's numeric Id is gone
public record WarehouseResponseV2(
    string Code,
    string Name,
    string Location,
    bool IsOperational);
