using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdventureWorks.Core;

namespace AdventureWorks;

// What the order API returns: no owner ID, and the Version
// travels in the ETag header instead of the body.
public sealed record OrderLineDto(
    int ProductId, int Quantity, decimal UnitPrice);

public sealed record OrderDto(int Id, string OrderNumber,
    string Status, bool Archived, decimal Total, string? Notes,
    IReadOnlyList<OrderLineDto> Items)
{
    public static OrderDto From(Order order) => new(order.Id,
        order.OrderNumber, order.Status, order.Archived,
        order.Total, order.Notes,
        [.. order.Items.Select(i => new OrderLineDto(
            i.ProductId, i.Quantity, i.UnitPrice))]);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items,
    int Page, int PageSize, int TotalCount)
{
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// PATCH body: the one field a customer may change.
public sealed class OrderNotesPatch
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

// Serialization code generated at build time for the order API's
// types. AddOrderServices puts it first in the resolver chain;
// reflection still serves every other type.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(PagedResponse<OrderSummary>))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(OrderNotesPatch))]
public sealed partial class StoreJsonContext
    : JsonSerializerContext
{
}
