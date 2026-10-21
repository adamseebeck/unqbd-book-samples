using Microsoft.EntityFrameworkCore;

public sealed class AddressValue
{
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
}
public sealed class ComplexCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressValue ShippingAddress { get; set; } = new();
}
public sealed class ComplexContext(DbContextOptions<ComplexContext> options) : DbContext(options)
{
    public DbSet<ComplexCustomer> Customers => Set<ComplexCustomer>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ComplexCustomer>().ComplexProperty(x => x.ShippingAddress);
        model.Entity<ComplexCustomer>().Property(x => x.ShippingAddress.PostalCode).HasMaxLength(20);
        model.Entity<ComplexCustomer>().HasIndex(x => x.ShippingAddress.PostalCode);
    }
}
public sealed class MigrationFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ComplexContext>
{
    public static string ReadLocalConnection()
    {
        var raw = Environment.GetEnvironmentVariable("EFCORE_BOOK_MIGRATION_CONNECTION")
            ?? throw new InvalidOperationException("Set EFCORE_BOOK_MIGRATION_CONNECTION for the local migration lab.");
        var c = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(raw);
        if (!System.Text.RegularExpressions.Regex.IsMatch(c.DataSource,
                @"^\(localdb\)\\[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            || !System.Text.RegularExpressions.Regex.IsMatch(c.InitialCatalog, @"^EfCoreBook_Migrations_[A-Za-z0-9_]+$")
            || !c.IntegratedSecurity)
            throw new InvalidOperationException("Use an integrated-auth LocalDB EfCoreBook_Migrations_ database.");
        return c.ConnectionString;
    }
    public ComplexContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<ComplexContext>().UseSqlServer(ReadLocalConnection()).Options);
}


public static class Program
{
    public static async Task Main(string[] args)
    {
        var instance = Environment.GetEnvironmentVariable("EFCORE_BOOK_LOCALDB") ?? @"(localdb)\MSSQLLocalDB";
        if (!System.Text.RegularExpressions.Regex.IsMatch(instance, @"^\(localdb\)\\[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Use LocalDB.");
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder { DataSource = instance,
            InitialCatalog = "EfCoreBook_Complex_" + Guid.NewGuid().ToString("N"), IntegratedSecurity = true,
            Encrypt = true, TrustServerCertificate = true };
        var migrationMode = args.Contains("--migrations");
        if (migrationMode) connection = new(MigrationFactory.ReadLocalConnection());
        var options = new DbContextOptionsBuilder<ComplexContext>().UseSqlServer(connection.ConnectionString).Options;
        Console.WriteLine($"Local lab database: {connection.InitialCatalog}");
        await using var db = new ComplexContext(options);
        try
        {
            if (migrationMode) await db.Database.MigrateAsync();
            else await db.Database.EnsureCreatedAsync();
            if (!await db.Customers.AnyAsync())
                db.Customers.Add(new ComplexCustomer { Name = "Alex", ShippingAddress = new AddressValue { Street = "1 Sample", PostalCode = "12345" } });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var row = await db.Customers.SingleAsync(x => x.ShippingAddress.PostalCode == "12345");
            if (row.ShippingAddress.Street != "1 Sample") throw new InvalidOperationException("Complex round-trip failed.");
            Console.WriteLine("PASS: EF Core 11 complex property lambda chaining, nested index and SQL Server round-trip");
        }
        finally { if (!migrationMode) await db.Database.EnsureDeletedAsync(); }
    }
}
