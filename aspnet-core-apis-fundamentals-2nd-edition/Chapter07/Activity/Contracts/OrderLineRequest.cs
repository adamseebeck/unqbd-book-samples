using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record OrderLineRequest(
    [Required] string Sku,
    [Range(1, int.MaxValue)] int Quantity
);
