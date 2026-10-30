using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // The same routes the orders controller served, so they stay mapped once
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapGet("/", async (
            OrderStatus? status,
            IOrderService service,
            CancellationToken ct,
            [Range(1, 100_000)] int page = 1,
            [Range(1, 100)] int pageSize = 10) =>
            Results.Ok(await service.GetOrdersAsync(status, page, pageSize, ct)));

        orders.MapGet("/{id:guid}", async (Guid id, IOrderService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } order
                ? Results.Ok(order)
                : Results.NotFound());

        orders.MapPost("/", async (
            CreateOrderRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IOrderService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.Problem("The Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest, title: "Missing Idempotency Key");
            }

            try
            {
                var order = await service.CreateAsync(request, idempotencyKey, ct);
                return order is null
                    ? Results.Problem("Idempotency key reused with a different request payload.", statusCode: StatusCodes.Status422UnprocessableEntity, title: "Idempotency Key Reused")
                    : Results.Created($"/api/orders/{order.Id}", order);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Order Rejected");
            }
        });

        orders.MapPost("/{id:guid}/allocate", async (
            Guid id,
            AllocateOrderRequest request,
            IOrderService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.AllocateAsync(id, request.WarehouseCode, ct));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Allocation Rejected");
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Allocation Failed");
            }
        });

        orders.MapPost("/{id:guid}/fulfill", async (Guid id, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.FulfillAsync(id, ct));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Fulfillment Failed");
            }
        });

        orders.MapPost("/{id:guid}/cancel", async (Guid id, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.CancelAsync(id, ct));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Cancellation Failed");
            }
        });

        orders.MapDelete("/{id:guid}", async (Guid id, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                return await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Order Cannot Be Deleted");
            }
        });

        return app;
    }
}
