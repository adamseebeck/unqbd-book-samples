using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WarehouseApi.HealthChecks;

// A health report is a status document, not an error: one JSON shape for every status.
// MapHealthChecks sets the status code (503 for Unhealthy) before the writer runs
public static class HealthReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // The build that answers: the Dockerfile passes the commit to dotnet publish as SourceRevisionId, which the
    // informational version carries after a +, so a deployment can wait for its own build (Chapter16/ci)
    private static readonly string Version = typeof(HealthReportWriter).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var body = new
        {
            status = report.Status.ToString(),
            version = Version,
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                // A check that throws reports the exception message, which can name servers and databases:
                // the logs keep it, the report says only that the check failed
                description = entry.Value.Exception is null ? entry.Value.Description : "The check failed; see the logs.",
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds),
                data = entry.Value.Data
            })
        };

        return context.Response.WriteAsJsonAsync(body, JsonOptions, context.RequestAborted);
    }
}
