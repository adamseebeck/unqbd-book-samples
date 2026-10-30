using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

// Runs against SQL Server: idempotency, the binary collation and rowversion depend on it
public class StockTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Fact]
    public async Task Staff_ReadTheirOwnWarehouse()
    {
        var client = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-001");

        var page = await client.GetFromJsonAsync<PagedResult<StockItemResponse>>("/api/v2/warehouses/WH-001/stock");

        Assert.NotNull(page);
        Assert.Equal(["SKU-001", "SKU-002"], page.Items.Select(item => item.ProductSku));
    }

    [Fact]
    public async Task StaffOfAnotherWarehouse_Get403ProblemDetails()
    {
        var client = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-002");

        var response = await client.GetAsync("/api/v2/warehouses/WH-001/stock");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task RequestsWithoutCredentials_Get401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v2/warehouses/WH-001/stock");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheStockList_GivesAnIntegrationClientItsOwnWarehouseOnly()
    {
        var client = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-002");

        var page = await client.GetFromJsonAsync<PagedResult<StockListItem>>("/api/v2/stock");
        var otherWarehouse = await client.GetAsync("/api/v2/stock?warehouseCode=WH-001");

        Assert.NotNull(page);
        Assert.All(page.Items, item => Assert.Equal("WH-002", item.WarehouseCode));
        Assert.Equal(HttpStatusCode.Forbidden, otherWarehouse.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRequestsWithOneIdempotencyKey_ApplyTheAdjustmentOnce()
    {
        var client = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-002");
        var before = await GetStockAsync(client, "WH-002", "SKU-001");

        var responses = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => AdjustAsync(client, "WH-002", "SKU-001", 10, "correction-2001")));

        // One request applied the change; the others replayed its stored 200 response
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(before.OnHand + 10, (await GetStockAsync(client, "WH-002", "SKU-001")).OnHand);
    }

    [Fact]
    public async Task KeysThatDifferOnlyInCase_AreTwoRequests()
    {
        // The idempotency key column uses a binary collation, so SQL Server compares keys exactly
        var client = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-001");
        var before = await GetStockAsync(client, "WH-001", "SKU-002");

        var first = await AdjustAsync(client, "WH-001", "SKU-002", 3, "Count-7A");
        var second = await AdjustAsync(client, "WH-001", "SKU-002", 3, "count-7a");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(before.OnHand + 6, (await GetStockAsync(client, "WH-001", "SKU-002")).OnHand);
    }

    [Fact]
    public async Task ASecondWriterWithAStaleRowVersion_IsRejected()
    {
        await using var first = factory.CreateDbContext();
        await using var second = factory.CreateDbContext();
        var itemA = await first.StockItems.SingleAsync(s => s.WarehouseCode == "WH-001" && s.ProductSku == "SKU-001");
        var itemB = await second.StockItems.SingleAsync(s => s.WarehouseCode == "WH-001" && s.ProductSku == "SKU-001");

        itemA.OnHand += 1;
        await first.SaveChangesAsync();
        itemB.OnHand += 5;

        // SQL Server changed the rowversion with the first update, so the second update matches no row
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private static async Task<StockItemResponse> GetStockAsync(HttpClient client, string warehouseCode, string sku) =>
        (await client.GetFromJsonAsync<StockItemResponse>($"/api/v2/warehouses/{warehouseCode}/stock/{sku}"))!;

    private static Task<HttpResponseMessage> AdjustAsync(HttpClient client, string warehouseCode, string sku, int change, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/warehouses/{warehouseCode}/stock/{sku}/adjustments")
        {
            Content = JsonContent.Create(new { quantityChange = change, reasonCode = "CORRECTION" })
        };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}
