using System.Text.Json.Serialization;

namespace WarehouseApi.Contracts;

// Read and written as names ("Receipt") in JSON rather than numbers
[JsonConverter(typeof(JsonStringEnumConverter<StockAdjustmentReason>))]
public enum StockAdjustmentReason
{
    Receipt,
    Damage,
    Correction,
    CycleCount
}
