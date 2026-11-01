using Microsoft.EntityFrameworkCore;

public sealed class OperationMiddleware(RequestDelegate next)
{
    private static int lastId;

    public async Task InvokeAsync(
        HttpContext http, InventoryDbContext db)
    {
        http.Items["OperationId"] =
            Interlocked.Increment(ref lastId);
        if (!http.Request.Path.StartsWithSegments("/health"))
            http.Items["Products"] = await db.Products
                .CountAsync(http.RequestAborted);
        await next(http);
    }
}
