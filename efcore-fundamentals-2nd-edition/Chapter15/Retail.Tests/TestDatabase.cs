using Microsoft.EntityFrameworkCore;

public sealed class TestDatabase
{
    private readonly string connection =
        @"Server=(localdb)\MSSQLLocalDB;" +
        "Integrated Security=true;TrustServerCertificate=true;" +
        $"Database=EfCoreBook_Test_{Guid.NewGuid():N}";

    public InventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(connection)
            .Options);

    public async Task CreateAsync()
    {
        await using var db = CreateContext();
        await RetailDatabase.ResetAsync(db, Seed.ProductAsync);
    }

    public async Task DeleteAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
