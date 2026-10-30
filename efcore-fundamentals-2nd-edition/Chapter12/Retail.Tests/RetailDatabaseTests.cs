using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class RetailDatabaseTests : IAsyncLifetime
{
    private readonly TestDatabase database = new();

    public Task InitializeAsync() => database.CreateAsync();
    public Task DisposeAsync() => database.DeleteAsync();

    [Fact]
    public async Task FreshContextReadsSavedProduct()
    {
        await using var db = database.CreateContext();
        var row = Assert.Single(
            await db.Products.AsNoTracking().ToListAsync());
        Assert.Equal("BOOK-001", row.Sku);
        Assert.Equal(10, row.StockQuantity);
    }

    [Fact]
    public async Task DuplicateSkuIsRejected()
    {
        await using var db = database.CreateContext();
        var categoryId = await db.Categories
            .Select(c => c.Id).SingleAsync();
        db.Products.Add(new Product
        {
            Sku = "BOOK-001", Name = "Duplicate", Price = 1m,
            CategoryId = categoryId
        });
        var error = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());
        var sql = Assert.IsType<SqlException>(
            error.InnerException);
        Assert.Contains(sql.Number, new[] { 2601, 2627 });
    }

    [Fact]
    public async Task StaleRowVersionRejectsSecondWriter()
    {
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var a = await first.Products.SingleAsync();
        var b = await second.Products.SingleAsync();
        a.StockQuantity -= 2;
        await first.SaveChangesAsync();
        b.StockQuantity -= 3;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => second.SaveChangesAsync());

        await using var read = database.CreateContext();
        var saved = await read.Products.SingleAsync();
        Assert.Equal(8, saved.StockQuantity);
    }

    [Fact]
    public async Task RollbackRemovesASavedChange()
    {
        await using (var db = database.CreateContext())
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();
            var product = await db.Products.SingleAsync();
            product.StockQuantity = 1;
            await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var read = database.CreateContext();
        var saved = await read.Products.SingleAsync();
        Assert.Equal(10, saved.StockQuantity);
    }

    [Fact]
    public async Task ReaderReturnsOnlyLowStockRows()
    {
        await using var db = database.CreateContext();
        var reader = new InventoryReader(db);
        Assert.Empty(await reader.LowStockAsync(threshold: 10));
        var row = Assert.Single(
            await reader.LowStockAsync(threshold: 11));
        Assert.Equal("C# Notebook", row.Name);
    }
}
