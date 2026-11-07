using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks;

// Activity: an order's lines as a paged sub-collection, with the
// same Link header and bounds as the orders collection.
public static class OrderItemEndpoints
{
    public static IEndpointRouteBuilder MapOrderItemEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:int}/items", ListItems)
            .WithTags("Orders").WithName("ListOrderItems");
        return app;
    }

    static async Task<IResult> ListItems(int id,
        IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        LinkGenerator links,
        [Range(1, 1000)] int page = 1,
        [Range(1, 50)] int pageSize = 10)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        if (!access.Succeeded) return Results.Forbid();
        OrderLineDto[] items = [.. order.Items
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new OrderLineDto(
                i.ProductId, i.Quantity, i.UnitPrice))];
        var body = new PagedResponse<OrderLineDto>(items, page,
            pageSize, order.Items.Count);
        if (page < body.TotalPages)
        {
            // The link keeps the order ID and the page size.
            var next = links.GetUriByName(context,
                "ListOrderItems",
                new { id, page = page + 1, pageSize });
            context.Response.Headers.Link =
                $"<{next}>; rel=\"next\"";
        }
        return Results.Ok(body);
    }
}
