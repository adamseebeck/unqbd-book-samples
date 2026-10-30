using System.ComponentModel.DataAnnotations;
using WarehouseApi.Validation;

namespace WarehouseApi.Contracts;

public record CreateProductRequest(
    [Required, ValidSku] string Sku,
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    [Range(0.01, 999999.99)] decimal UnitPrice);
