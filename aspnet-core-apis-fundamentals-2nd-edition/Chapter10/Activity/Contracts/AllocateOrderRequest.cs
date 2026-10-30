using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record AllocateOrderRequest(
    [Required, StringLength(10)] string WarehouseCode
);
