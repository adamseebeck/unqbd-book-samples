using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// Chapter 16: the counter a dashboard would read, checked through
// the app's own IMeterFactory, so no exporter is needed.
[Trait("Category", "Integration")]
public sealed class OrderTelemetryTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task APlacedOrderIsCounted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);
        using var created = new MetricCollector<long>(
            factory.Services.GetRequiredService<IMeterFactory>(),
            OrderTelemetry.Name, "orders.created");

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 2,
                Quantity = 1 } } }, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, created.GetMeasurementSnapshot()
            .EvaluateAsCounter());
    }
}
