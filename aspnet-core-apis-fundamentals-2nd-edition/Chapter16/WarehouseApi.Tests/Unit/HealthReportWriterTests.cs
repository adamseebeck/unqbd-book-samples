using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WarehouseApi.HealthChecks;

namespace WarehouseApi.Tests.Unit;

// The readiness report is public: a check that threw must not pass its exception message on
public class HealthReportWriterTests
{
    [Fact]
    public async Task ACheckThatThrew_ReportsAFixedDescription_NotTheExceptionMessage()
    {
        var failure = new InvalidOperationException("Login failed on server sql-prod-7.internal");
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(HealthStatus.Unhealthy, failure.Message, TimeSpan.FromMilliseconds(12), failure, null)
            },
            TimeSpan.FromMilliseconds(12));
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthReportWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal("The check failed; see the logs.",
            json.RootElement.GetProperty("checks")[0].GetProperty("description").GetString());
        Assert.DoesNotContain("sql-prod-7", body);
    }
}
