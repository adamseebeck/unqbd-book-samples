using System.ComponentModel.DataAnnotations;
using WarehouseApi.Validation;

namespace WarehouseApi.Contracts;

public record OrderLineRequest(
    [Required, ValidSku] string Sku,
    [Range(1, 10000)] int Quantity
);
