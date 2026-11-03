using System.Text;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

// Chapter 17: the Admin posts a price feed as text. The app
// reads it as a stream, checks and counts it, and saves nothing.
// Kestrel refuses a body over 64 KB with 413.
public static class ProductImport
{
    public static IEndpointRouteBuilder MapProductImport(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/products/import", ImportAsync)
            .RequireAuthorization(StorePolicies.AdminOnly)
            .WithMetadata(
                new RequestSizeLimitAttribute(64 * 1024));
        return app;
    }

    static async Task<IResult> ImportAsync(HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();
        try
        {
            var records = await PriceFeed.CountAsync(
                context.Request.Body, ct);
            return Results.Ok(new { Records = records });
        }
        catch (Exception ex) when (ex is FormatException
            or DecoderFallbackException)
        {
            return Results.Problem(ex.Message, statusCode:
                StatusCodes.Status400BadRequest);
        }
    }
}
