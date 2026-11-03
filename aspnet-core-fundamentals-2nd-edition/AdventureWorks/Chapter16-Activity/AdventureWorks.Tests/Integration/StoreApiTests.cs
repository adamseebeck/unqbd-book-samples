using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// The real pipeline, endpoints and EF Core code, over HTTP in
// memory. One factory (one app, one database) for the class.
[Trait("Category", "Integration")]
public sealed class StoreApiTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task OrdersNeedASignedInUser()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task APlacedOrderHasALocationAndAnETag()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 4,
                Quantity = 1 } } }, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var order = await client.GetFromJsonAsync<JsonElement>(
            response.Headers.Location, ct);
        Assert.Equal(189.00m,
            order.GetProperty("total").GetDecimal());
    }
}
