namespace AdventureWorks;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Redirect("/api/status"));
        app.MapGet("/api/status", () => Results.Ok(new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow
        }));
        return app;
    }
}
