using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record OrderSearchRequest(
    OrderStatus? Status,
    string? CustomerReference,
    [Range(1, 100_000)] int PageNumber = 1,
    [Range(1, 100)] int PageSize = 10
);
