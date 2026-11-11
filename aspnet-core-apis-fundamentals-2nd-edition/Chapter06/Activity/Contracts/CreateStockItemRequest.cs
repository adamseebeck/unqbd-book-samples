using System.Text.Json.Serialization;

namespace WarehouseApi.Contracts;

public record CreateStockItemRequest(
    string ProductSku,
    [property: JsonRequired] int OnHand,
    [property: JsonRequired] int Reserved,
    [property: JsonRequired] int ReorderThreshold);
