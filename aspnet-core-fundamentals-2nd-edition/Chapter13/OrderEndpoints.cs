public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");
        group.MapGet("/", (HttpContext context, [AsParameters] PaginationRequest request,
            IOrderService service, LinkGenerator links) =>
        {
            if (request.PageNumber < 1 || request.PageNumber > 1_000_000 ||
                request.PageSize is < 1 or > 100 ||
                request.SortDirection is not ("asc" or "desc"))
                return Results.Problem(statusCode: 400, title: "Invalid pagination",
                    detail: "Use pageNumber 1..1000000, pageSize 1..100, asc or desc.");
            var page = service.GetPage(request);
            var next = links.GetPathByName("GetOrders", new
            {
                pageNumber = request.PageNumber + 1, pageSize = request.PageSize,
                customerName = request.CustomerName, sortDirection = request.SortDirection
            });
            if (page.PageNumber < page.TotalPages && next is not null)
                context.Response.Headers.Link = $"<{next}>; rel=\"next\"";
            context.Response.Headers["X-Total-Count"] = page.TotalCount.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return Results.Ok(page);
        }).WithName("GetOrders");

        group.MapGet("/{id:guid}", (Guid id, IOrderService service) =>
            service.Get(id) is { } order ? Results.Ok(order) :
                Results.Problem(statusCode: 404, title: "Order not found"))
            .WithName("GetOrderById");

        group.MapPost("/", (CreateOrderRequest request, IOrderService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName) ||
                request.CustomerName.Length > 100 || request.Items is not { Length: > 0 and <= 100 } ||
                request.Items.Any(i => i is null || i.ProductId < 1 || i.Quantity is < 1 or > 1000))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["order"] = ["Supply a customer and 1..100 valid items."]
                });
            var created = service.Create(request);
            return Results.CreatedAtRoute("GetOrderById", new { id = created.Id }, created);
        });

        group.MapPatch("/{id:guid}", (Guid id, UpdateOrderRequest request, IOrderService service) =>
        {
            if (request.CustomerName is not null &&
                (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100))
                return Results.Problem(statusCode: 400, title: "Invalid customer name");
            return service.Update(id, request) ? Results.NoContent() :
                Results.Problem(statusCode: 404, title: "Order not found");
        });

        group.MapDelete("/{id:guid}", (Guid id, IOrderService service) =>
            service.Delete(id) ? Results.NoContent() :
                Results.Problem(statusCode: 404, title: "Order not found"));
    }
}
