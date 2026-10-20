using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

public sealed record OrderLineItemDto(int ProductId, int Quantity);
public sealed record OrderDto(Guid Id, string CustomerName, DateTimeOffset OrderDate,
    IReadOnlyList<OrderLineItemDto> Items);
public sealed record CreateOrderRequest(string CustomerName, OrderLineItemDto[] Items);
public sealed record UpdateOrderRequest(string? CustomerName, DateTimeOffset? OrderDate);
public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 10,
    string? CustomerName = null, string SortDirection = "asc");
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int PageNumber,
    int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(UpdateOrderRequest))]
[JsonSerializable(typeof(PagedResponse<OrderDto>))]
[JsonSerializable(typeof(PagedResponse<OrderLineItemDto>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(string))]
public partial class ApiJsonContext : JsonSerializerContext { }
