using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record CreateOrderRequest(
    [Required]
    [StringLength(50)]
    string CustomerReference,

    [StringLength(10)]
    string? FulfillmentWarehouseCode,

    [Required]
    [MinLength(1)]
    IReadOnlyList<OrderLineRequest> Lines
);
