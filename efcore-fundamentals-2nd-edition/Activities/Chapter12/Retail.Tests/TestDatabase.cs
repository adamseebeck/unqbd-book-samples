using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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

    public IServiceProvider CreateServices() =>
        new ServiceCollection()
            .AddDbContext<InventoryDbContext>(options =>
                options.UseSqlServer(connection))
            .BuildServiceProvider();

    public async Task CreateAsync(
        Func<InventoryDbContext, Task>? seed = null)
    {
        await using var db = CreateContext();
        await RetailDatabase.ResetAsync(
            db, seed ?? (d => Seed.ProductAsync(d)));
    }

    public async Task DeleteAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
