namespace AdventureWorks;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapGet("/", () => Results.Redirect("/api/status"))
            .AllowAnonymous();
        app.MapGet("/api/status", () => Results.Ok(new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow
        })).AllowAnonymous();
        return app;
    }
}
