using System.Net;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// A factory of its own, so the failing stub and the circuit
// breaker it trips cannot affect the other test classes.
[Trait("Category", "Integration")]
public sealed class StockFailureTests : IDisposable
{
    private readonly StoreFactory _factory = new();

    [Fact]
    public async Task AFailingWarehouseIs503()
    {
        _factory.Inventory.Status =
            HttpStatusCode.ServiceUnavailable;
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/catalog/2/availability",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        // One call and two retries.
        Assert.Equal(3, _factory.Inventory.Calls);
    }

    public void Dispose() => _factory.Dispose();
}
