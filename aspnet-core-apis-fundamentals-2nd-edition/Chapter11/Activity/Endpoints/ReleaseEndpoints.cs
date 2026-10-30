using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/releases", async (
            ReleaseRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IStockReleaseService releaseService,
            CancellationToken cancellationToken) =>
        {
            // A release is not idempotent, so a retry needs a key, as a stock adjustment does
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > IdempotencyKeyRules.MaxLength)
            {
                return Results.Problem(
                    $"An Idempotency-Key header of at most {IdempotencyKeyRules.MaxLength} characters is required.",
                    statusCode: StatusCodes.Status400BadRequest, title: "Release Rejected");
            }

            // Other exceptions reach the exception handling middleware from Chapter 8
            try
            {
                var order = await releaseService.ReleaseAsync(request, idempotencyKey, cancellationToken);
                return order is null
                    ? Results.Problem("Idempotency key reused with a different request payload.",
                        statusCode: StatusCodes.Status422UnprocessableEntity, title: "Release Rejected")
                    : Results.Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                // The body names the order and the line; the URL names /api/releases, which exists, so 422, not 404
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Release Rejected");
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Release Failed");
            }
        });

        return app;
    }
}
