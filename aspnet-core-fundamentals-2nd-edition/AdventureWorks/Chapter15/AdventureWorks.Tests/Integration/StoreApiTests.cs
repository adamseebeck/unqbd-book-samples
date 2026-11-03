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
    static CancellationToken Ct =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheCatalogIsPublic()
    {
        var client = factory.CreateClient();

        var catalog = await client.GetFromJsonAsync<JsonElement>(
            "/api/catalog", Ct);

        Assert.Equal(4, catalog.GetProperty("items")
            .GetArrayLength());
    }

    [Fact]
    public async Task OrdersNeedASignedInUser()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task APlacedOrderHasALocationAndAnETag()
    {
        var client = factory.CreateCustomer();
        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/csrf", Ct);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",
            token.GetProperty("token").GetString());

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 4,
                Quantity = 1 } } }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var order = await client.GetFromJsonAsync<JsonElement>(
            response.Headers.Location, Ct);
        Assert.Equal(189.00m,
            order.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task StockComesFromTheStub()
    {
        var client = factory.CreateClient();

        var stock = await client.GetFromJsonAsync<JsonElement>(
            "/api/catalog/1/availability", Ct);

        Assert.Equal(7,
            stock.GetProperty("available").GetInt32());
        Assert.True(factory.Inventory.Calls >= 1);
    }
}
