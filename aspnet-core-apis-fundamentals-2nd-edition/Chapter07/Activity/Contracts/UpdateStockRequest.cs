using System.Text.Json.Serialization;

namespace WarehouseApi.Contracts;

public record UpdateStockRequest(
    [property: JsonRequired] int OnHand,
    [property: JsonRequired] int Reserved,
    [property: JsonRequired] int ReorderThreshold);
