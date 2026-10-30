using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record UpdateProductRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    [Range(0.01, 999999.99)] decimal UnitPrice,
    // Nullable, so [Required] can reject a PUT that omits it; a plain bool would bind false
    [Required] bool? IsActive);
