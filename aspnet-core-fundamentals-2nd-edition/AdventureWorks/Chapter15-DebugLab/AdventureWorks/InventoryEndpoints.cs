using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Antiforgery;

namespace AdventureWorks;

// Stock levels and transfers, for the store's Admin.
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(
        this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/api/admin/inventory")
            .WithTags("Inventory")
            .RequireAuthorization(StorePolicies.AdminOnly);
        inventory.MapGet("/{productId:int}", async (
            int productId, IInventoryRepository stock) =>
            Results.Ok(await stock.GetAsync(productId)));
        inventory.MapPost("/transfer", Transfer);
        return app;
    }

    static async Task<IResult> Transfer(StockTransfer transfer,
        IInventoryRepository stock, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();
        return await stock.TransferAsync(transfer) switch
        {
            TransferOutcome.Moved => Results.Ok(
                await stock.GetAsync(transfer.ProductId)),
            TransferOutcome.NotEnoughStock => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Not enough stock",
                detail: $"{transfer.From} has fewer than "
                    + $"{transfer.Quantity}."),
            _ => Results.NotFound()
        };
    }
}
