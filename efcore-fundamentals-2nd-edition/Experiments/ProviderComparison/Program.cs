using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

public sealed class SqliteCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class SqliteProduct
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public SqliteCategory Category { get; set; } = null!;
    public Guid Version { get; set; }
}
public sealed class SqliteContext(DbContextOptions<SqliteContext> options) : DbContext(options)
{
    public DbSet<SqliteProduct> Products => Set<SqliteProduct>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SqliteProduct>().HasIndex(x => x.Sku).IsUnique();
        model.Entity<SqliteProduct>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<SqliteProduct>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
    }
    private void StampVersions()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<SqliteProduct>())
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.Version = Guid.NewGuid();
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
}
public static class Program
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine($"PASS: SQLite {name}");
    }
    public static async Task Main()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteContext>().UseSqlite(connection).Options;
        await using (var setup = new SqliteContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Add(new SqliteProduct { Sku = "ONE", Stock = 10, Category = new SqliteCategory { Name = "Books" } });
            await setup.SaveChangesAsync();
        }
        await using (var invalid = new SqliteContext(options))
        {
            invalid.Add(new SqliteProduct { Sku = "BAD", Stock = 1, CategoryId = int.MaxValue });
            var rejected = false;
            try { await invalid.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException e && e.SqliteErrorCode == 19) { rejected = true; }
            Check(rejected, "foreign-key constraint is relational");
        }
        await using (var writer = new SqliteContext(options))
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            var p = await writer.Products.SingleAsync(); p.Stock = 2; await writer.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await using (var read = new SqliteContext(options)) Check((await read.Products.SingleAsync()).Stock == 10, "explicit rollback is visible to fresh context");
        await using var a = new SqliteContext(options);
        await using var b = new SqliteContext(options);
        var first = await a.Products.SingleAsync(); var second = await b.Products.SingleAsync();
        var originalToken = first.Version;
        first.Stock = 8; a.SaveChanges(); second.Stock = 7;
        Check(first.Version != originalToken, "synchronous save advances the application-managed token");
        var conflict = false;
        try { await b.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflict = true; }
        Check(conflict, "stale async save conflicts with a synchronous first writer");
        await using var fresh = new SqliteContext(options);
        Check((await fresh.Products.SingleAsync()).Stock == 8, "conflicting async writer preserves synchronous stock");
        Console.WriteLine("SQLite observations do not prove SQL Server rowversion, SQL or collation behavior.");
    }
}
