using System.ComponentModel.DataAnnotations;
using WarehouseApi.Validation;

namespace WarehouseApi.Contracts;

public record StockAdjustmentRequest(
    // Nullable, so [Required] can reject a body without it; an int would bind 0
    [Required, Range(-10000, 10000)] int? QuantityChange,
    [Required, ValidReasonCode] string ReasonCode);
