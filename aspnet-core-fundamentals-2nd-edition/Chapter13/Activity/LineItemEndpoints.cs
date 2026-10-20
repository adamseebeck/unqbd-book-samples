public static class LineItemEndpoints
{
    public static void MapOrderLineItems(this WebApplication app)
    {
        app.MapGet("/api/orders/{orderId:guid}/items", (Guid orderId,
            [AsParameters] PaginationRequest request, IOrderService service,
            HttpContext context, LinkGenerator links) =>
        {
            if (request.PageNumber is < 1 or > 1_000_000 || request.PageSize is < 1 or > 100)
                return Results.Problem(statusCode: 400, title: "Invalid pagination");
            var order = service.Get(orderId);
            if (order is null) return Results.Problem(statusCode: 404, title: "Order not found");
            var page = new PagedResponse<OrderLineItemDto>(order.Items
                .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToArray(),
                request.PageNumber, request.PageSize, order.Items.Count);
            if (request.PageNumber < page.TotalPages)
            {
                var next = links.GetPathByName("GetOrderLineItems", new
                {
                    orderId, pageNumber = request.PageNumber + 1, pageSize = request.PageSize
                });
                context.Response.Headers.Link = $"<{next}>; rel=\"next\"";
            }
            return Results.Ok(page);
        }).WithName("GetOrderLineItems");
    }
}
