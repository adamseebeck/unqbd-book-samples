using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WarehouseApi.Fulfillment;
using WarehouseApi.Persistence;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class HealthCheckTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    // Nothing listens on port 1, so the connection fails at once
    private const string UnreachableServer =
        "Server=tcp:127.0.0.1,1;Database=WarehouseApiTests_Unreachable;Integrated Security=true;" +
        "Encrypt=true;TrustServerCertificate=true;Connect Timeout=2";

    [Fact]
    public async Task Readiness_ReportsTheDatabaseAndTheQueue()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", report.RootElement.GetProperty("status").GetString());
        // The build that answers, so a deployment can wait for its own commit
        Assert.False(string.IsNullOrEmpty(report.RootElement.GetProperty("version").GetString()));
        Assert.Equal(["database", "fulfillment-queue"],
            report.RootElement.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Readiness_Returns503WhenTheDatabaseIsUnreachable_WhileLivenessAndTheProcessorKeepRunning()
    {
        await using var broken = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            // Since EF Core 9, AddDbContext registers the options and, separately, the code that configures
            // them. Remove both, or the original configuration still runs before the replacement
            services.RemoveAll<DbContextOptions<WarehouseDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<WarehouseDbContext>>();
            services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(UnreachableServer));
        }));
        var client = broken.CreateClient();
        var lifetime = broken.Services.GetRequiredService<IHostApplicationLifetime>();
        var queue = broken.Services.GetRequiredService<IFulfillmentJobQueue>();

        // The processor reads the queue only after its startup sweep, which fails without the database. Once
        // it has taken this job, that failure is behind it, and the host must still be running
        Assert.True(queue.TryEnqueue(new FulfillmentJob(Guid.NewGuid(), "startup-sweep-test", DateTimeOffset.UtcNow)));
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (queue.Count > 0 && !lifetime.ApplicationStopping.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        var readiness = await client.GetAsync("/health");
        var liveness = await client.GetAsync("/health/live");
        var body = await readiness.Content.ReadAsStringAsync();
        using var report = JsonDocument.Parse(body);

        Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);
        Assert.Equal(0, queue.Count);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal("Unhealthy", report.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("127.0.0.1", body);
    }
}
