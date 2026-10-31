using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class OrderServiceTests : IAsyncLifetime
{
    private readonly TestDatabase database = new();

    public Task InitializeAsync() =>
        database.CreateAsync(Seed.CatalogAsync);
    public Task DisposeAsync() => database.DeleteAsync();

    [Fact]
    public async Task PlaceAsyncReducesStock()
    {
        await using (var db = database.CreateContext())
        {
            var (customerId, notebookId) = await IdsAsync(db);
            await OrderService.PlaceAsync(
                db, customerId, notebookId, quantity: 2);
        }

        await using var read = database.CreateContext();
        var notebook = await read.Products
            .SingleAsync(p => p.Sku == "BOOK-001");
        Assert.Equal(8, notebook.StockQuantity);
        Assert.Equal(2, await read.Orders.CountAsync());
    }

    [Fact]
    public async Task PlaceAsyncRefusesMoreThanStock()
    {
        await using (var db = database.CreateContext())
        {
            var (customerId, notebookId) = await IdsAsync(db);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => OrderService.PlaceAsync(
                    db, customerId, notebookId, quantity: 11));
        }

        await using var read = database.CreateContext();
        var notebook = await read.Products
            .SingleAsync(p => p.Sku == "BOOK-001");
        Assert.Equal(10, notebook.StockQuantity);
        Assert.Equal(1, await read.Orders.CountAsync());
    }

    private static async Task<(int, int)> IdsAsync(
        InventoryDbContext db) =>
        (await db.Customers.Select(c => c.Id).SingleAsync(),
         await db.Products.Where(p => p.Sku == "BOOK-001")
            .Select(p => p.Id).SingleAsync());
}
