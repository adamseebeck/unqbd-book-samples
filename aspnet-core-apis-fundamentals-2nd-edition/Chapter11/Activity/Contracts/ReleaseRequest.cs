using System.ComponentModel.DataAnnotations;
using WarehouseApi.Validation;

namespace WarehouseApi.Contracts;

// Releases part of one line's reservation, for example when a customer orders fewer units
public record ReleaseRequest(
    // Nullable, so [Required] can reject a body without it; a Guid would bind Guid.Empty
    [Required] Guid? OrderId,
    [Required, ValidSku] string Sku,
    [Range(1, 10000)] int Quantity);
