using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record UpdateStockRequest(
    [Range(0, int.MaxValue)] int OnHand,
    [Range(0, int.MaxValue)] int Reserved,
    [Range(0, int.MaxValue)] int ReorderThreshold);
