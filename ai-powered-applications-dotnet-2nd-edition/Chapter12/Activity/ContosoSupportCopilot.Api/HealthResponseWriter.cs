using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ContosoSupportCopilot.Api;

// The health report as JSON: an overall status and one status per dependency, never exception details.
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var response = new
        {
            Status = report.Status.ToString(),
            CheckedAt = DateTime.UtcNow,
            Dependencies = report.Entries
                .SelectMany(entry => entry.Value.Data.Count > 0
                    ? entry.Value.Data.Select(d => KeyValuePair.Create(d.Key, d.Value.ToString()))
                    : [KeyValuePair.Create(entry.Key, (string?)entry.Value.Status.ToString())])
                .ToDictionary(pair => pair.Key, pair => pair.Value)
        };

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, Options));
    }
}
