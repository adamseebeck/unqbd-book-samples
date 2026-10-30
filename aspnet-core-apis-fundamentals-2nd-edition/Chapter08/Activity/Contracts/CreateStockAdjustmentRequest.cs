using System.ComponentModel.DataAnnotations;
using WarehouseApi.Validation;

namespace WarehouseApi.Contracts;

public record CreateStockAdjustmentRequest(
    [Required, ValidSku] string Sku,
    [Required, ValidWarehouseCode] string WarehouseCode,
    [Required, Range(-10000, 10000)] int? QuantityChange,
    [Required, ValidReasonCode] string ReasonCode);
