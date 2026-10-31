using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class StockTransferTests : IAsyncLifetime
{
    private readonly TestDatabase database = new();

    public Task InitializeAsync() =>
        database.CreateAsync(Seed.CatalogAsync);
    public Task DisposeAsync() => database.DeleteAsync();

    [Fact]
    public async Task TransferMovesStock()
    {
        var (notebookId, padId) = await IdsAsync();
        await StockTransfer.RunAsync(database.CreateServices(),
            notebookId, padId, quantity: 1);

        Assert.Equal(9, await StockAsync(notebookId));
        Assert.Equal(3, await StockAsync(padId));
    }

    [Fact]
    public async Task TransferRefusesMoreThanSource()
    {
        var (notebookId, padId) = await IdsAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => StockTransfer.RunAsync(
                database.CreateServices(),
                padId, notebookId, quantity: 3));

        Assert.Equal(10, await StockAsync(notebookId));
        Assert.Equal(2, await StockAsync(padId));
    }

    private async Task<(int, int)> IdsAsync()
    {
        await using var db = database.CreateContext();
        return (await IdOfAsync(db, "BOOK-001"),
            await IdOfAsync(db, "PAD-002"));
    }

    private static Task<int> IdOfAsync(
        InventoryDbContext db, string sku) =>
        db.Products.Where(p => p.Sku == sku)
            .Select(p => p.Id).SingleAsync();

    private async Task<int> StockAsync(int id)
    {
        await using var db = database.CreateContext();
        return await db.Products.Where(p => p.Id == id)
            .Select(p => p.StockQuantity).SingleAsync();
    }
}
