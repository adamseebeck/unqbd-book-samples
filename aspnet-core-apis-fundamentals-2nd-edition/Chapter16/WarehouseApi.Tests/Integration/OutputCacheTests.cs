using System.Net;
using System.Net.Http.Json;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class OutputCacheTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Fact]
    public async Task TheCatalog_IsServedFromTheCache_WithTheCallersCorrelationId()
    {
        var client = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-001");

        var first = await GetAsync(client, "/api/v2/products?pageSize=5", "catalog-1");
        var second = await GetAsync(client, "/api/v2/products?pageSize=5", "catalog-2");

        // The output cache adds Age to a response it serves from the cache
        Assert.False(first.Headers.Contains("Age"));
        Assert.True(second.Headers.Contains("Age"));
        Assert.Equal("catalog-2", second.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task AddingAProduct_EvictsTheCachedCatalog()
    {
        var staff = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-001");
        var supervisor = factory.CreateClientAs(UserRoles.Supervisor);
        var before = await staff.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v2/products?pageSize=50");

        var created = await supervisor.PostAsJsonAsync("/api/v2/products",
            new CreateProductRequest("SKU-777", "Pallet Wrap", "Stretch film", 19.99m));
        var after = await staff.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v2/products?pageSize=50");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(before!.TotalCount + 1, after!.TotalCount);
        Assert.Contains(after.Items, product => product.Sku == "SKU-777");
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Correlation-ID", correlationId);
        return client.SendAsync(request);
    }
}
