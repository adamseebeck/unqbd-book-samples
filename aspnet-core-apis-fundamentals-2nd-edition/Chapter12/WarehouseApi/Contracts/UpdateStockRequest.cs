using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record UpdateStockRequest(
    [Range(0, int.MaxValue)] [property: JsonRequired] int OnHand,
    [Range(0, int.MaxValue)] [property: JsonRequired] int Reserved,
    [Range(0, int.MaxValue)] [property: JsonRequired] int ReorderThreshold);
