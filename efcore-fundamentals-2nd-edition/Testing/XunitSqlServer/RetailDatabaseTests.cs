using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Xunit;

// xUnit v2 creates one test-class instance per test. Each owns a separate database.
public sealed class RetailDatabaseTests : IAsyncLifetime
{
    private readonly LocalLab lab = new(12);

    public async Task InitializeAsync()
    {
        await using var db = lab.Create();
        await db.Database.EnsureCreatedAsync();
        await Seed.ProductAsync(db);
    }

    public Task DisposeAsync() => lab.DisposeAsync().AsTask();

    [Fact]
    public async Task FreshContextReadsSavedProduct()
    {
        await using var read = lab.Create();
        var row = Assert.Single(await read.Products.AsNoTracking().ToListAsync());
        Assert.Equal("BOOK-001", row.Sku);
        Assert.Equal(10, row.StockQuantity);
    }

    [Fact]
    public async Task UniqueSkuIsEnforcedBySqlServer()
    {
        await using var db = lab.Create();
        db.Products.Add(new Product
        {
            Name = "Duplicate", Sku = "BOOK-001", Price = 1m,
            CategoryId = await db.Categories.Select(c => c.Id).SingleAsync()
        });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Contains(sql.Number, new[] { 2601, 2627 });
    }

    [Fact]
    public async Task StaleRowversionRejectsSecondWriter()
    {
        await using var first = lab.Create();
        await using var second = lab.Create();
        var a = await first.Products.SingleAsync();
        var b = await second.Products.SingleAsync();
        a.StockQuantity -= 2;
        await first.SaveChangesAsync();
        b.StockQuantity -= 3;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var read = lab.Create();
        Assert.Equal(8, (await read.Products.SingleAsync()).StockQuantity);
    }

    [Fact]
    public async Task RollbackRemovesAnAlreadySavedChange()
    {
        await using (var writer = lab.Create())
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            var product = await writer.Products.SingleAsync();
            product.StockQuantity = 1;
            await writer.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await using var read = lab.Create();
        Assert.Equal(10, (await read.Products.SingleAsync()).StockQuantity);
    }
}
