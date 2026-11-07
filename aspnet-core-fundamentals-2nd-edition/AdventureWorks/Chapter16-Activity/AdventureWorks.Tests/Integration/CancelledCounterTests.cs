using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// Activity: the counter of cancelled orders.
[Trait("Category", "Integration")]
public sealed class CancelledCounterTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task ACancelledOrderIsCounted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);
        using var cancelled = new MetricCollector<long>(
            factory.Services.GetRequiredService<IMeterFactory>(),
            OrderTelemetry.Name, "orders.cancelled");
        var created = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 2,
                Quantity = 1 } } }, ct);

        var response = await client.DeleteAsync(
            created.Headers.Location, ct);

        Assert.Equal(HttpStatusCode.NoContent,
            response.StatusCode);
        Assert.Equal(1, cancelled.GetMeasurementSnapshot()
            .EvaluateAsCounter());
    }
}
