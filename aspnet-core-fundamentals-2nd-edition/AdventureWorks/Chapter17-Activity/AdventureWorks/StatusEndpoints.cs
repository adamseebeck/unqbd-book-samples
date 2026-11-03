using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdventureWorks;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(
        this IEndpointRouteBuilder app)
    {
        // Live: the process answers; no checks run.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();
        // Ready: the checks tagged "ready" pass. Degraded maps to
        // 200 by default; here it means not ready (503).
        var ready = new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        };
        ready.ResultStatusCodes[HealthStatus.Degraded] =
            StatusCodes.Status503ServiceUnavailable;
        app.MapHealthChecks("/health/ready", ready)
            .AllowAnonymous();
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
