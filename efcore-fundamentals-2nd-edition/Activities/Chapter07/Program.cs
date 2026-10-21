using Microsoft.EntityFrameworkCore;

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = [];
}
public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductCategory> ProductCategories { get; set; } = [];
}
public enum CustomerStatus { Active, Inactive, Suspended }
public enum OrderStatus { Pending, Completed, Cancelled }
public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public CustomerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Order> Orders { get; set; } = [];
    public CustomerProfile? Profile { get; set; }
}
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> OrderItems { get; set; } = [];
}
public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public sealed class ProductCategory
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int StockThreshold { get; set; }
}
public sealed class CustomerProfile
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string LoyaltyTier { get; set; } = "Standard";
}

public sealed class Supplier
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = [];
}
public sealed class SupplierConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<Supplier>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Supplier> b)
    {
        b.Property(x => x.CompanyName).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.CompanyName).IsClustered(false);
        b.Property(x => x.ContactEmail).IsRequired().HasMaxLength(255);
        b.HasIndex(x => x.ContactEmail).IsUnique().HasDatabaseName("IX_Suppliers_ContactEmail");
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
    }
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>().Property(x => x.Name).HasMaxLength(100);
        model.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        model.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        model.Entity<Product>().ToTable("Products");
        model.Entity<Product>().HasOne(x => x.Supplier).WithMany(x => x.Products)
            .HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Product>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<Product>().Property(x => x.Sku).HasMaxLength(40);
        model.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        model.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        model.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Customer>().Property(x => x.Email).HasMaxLength(255);
        model.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        model.Entity<Customer>().Property(x => x.Status).HasConversion<string>();
        model.Entity<Order>().Property(x => x.Status).HasConversion<string>();
        model.Entity<Order>().HasOne(x => x.Customer).WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        model.Entity<OrderItem>().HasOne(x => x.Order).WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrderItem>().HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ProductCategory>().HasKey(x => new { x.ProductId, x.CategoryId });
        model.Entity<ProductCategory>().HasOne(x => x.Product).WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<ProductCategory>().HasOne(x => x.Category).WithMany()
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerProfile>().HasKey(x => x.CustomerId);
        model.Entity<CustomerProfile>().HasOne(x => x.Customer).WithOne(x => x.Profile)
            .HasForeignKey<CustomerProfile>(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LocalLab : IAsyncDisposable
{
    public DbContextOptions<InventoryDbContext> Options { get; }
    public string ConnectionString { get; }
    public LocalLab(int chapter)
    {
        var instance = Environment.GetEnvironmentVariable("EFCORE_BOOK_LOCALDB")
            ?? @"(localdb)\MSSQLLocalDB";
        if (!System.Text.RegularExpressions.Regex.IsMatch(instance,
            @"^\(localdb\)\\[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Use a local LocalDB instance for these disposable labs.");
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = instance, InitialCatalog = $"EfCoreBook_Ch{chapter:00}_{Guid.NewGuid():N}",
            IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true
        };
        ConnectionString = connection.ConnectionString;
        Console.WriteLine($"Local lab database: {connection.InitialCatalog}");
        Options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseCompatibilityLevel(160)).Options;
    }
    public InventoryDbContext Create() => new(Options);
    public async ValueTask DisposeAsync()
    {
        await using var db = Create();
        await db.Database.EnsureDeletedAsync();
    }
}

public static class Seed
{
    public static async Task<Product> ProductAsync(InventoryDbContext db)
    {
        var product = new Product { Name = "C# Notebook", Sku = "BOOK-001", Price = 12.50m,
            StockQuantity = 10, Category = new Category { Name = "Books" } };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}


public sealed class MigrationFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<InventoryDbContext>
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
    public InventoryDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(ReadLocalConnection()).Options);
}



public static class Program
{
    public static async Task Main()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(MigrationFactory.ReadLocalConnection()).Options;
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.MigrateAsync();
            if (!await setup.Products.AnyAsync())
            {
                var product = await Seed.ProductAsync(setup);
                product.Supplier = new Supplier { CompanyName = "Active", ContactEmail = "active@example.test", CreatedAt = DateTime.UtcNow };
                setup.Add(new Supplier { CompanyName = "Inactive", ContactEmail = "inactive@example.test", CreatedAt = DateTime.UtcNow, IsActive = false });
                await setup.SaveChangesAsync();
            }
        }
        await using var eager = new InventoryDbContext(options);
        var suppliers = await eager.Set<Supplier>().Include(s => s.Products).OrderBy(s => s.Id).ToListAsync();
        if (suppliers.Count != 2 || suppliers.Sum(s => s.Products.Count) != 1) throw new InvalidOperationException("Eager report failed.");
        await using var conditional = new InventoryDbContext(options);
        var active = await conditional.Set<Supplier>().SingleAsync(s => s.IsActive);
        if (active.IsActive) await conditional.Entry(active).Collection(s => s.Products).LoadAsync();
        var inactive = await conditional.Set<Supplier>().SingleAsync(s => !s.IsActive);
        if (!conditional.Entry(active).Collection(s => s.Products).IsLoaded ||
            conditional.Entry(inactive).Collection(s => s.Products).IsLoaded)
            throw new InvalidOperationException("Conditional loading state differs.");
        await using (var deleting = new InventoryDbContext(options))
        {
            deleting.Remove(await deleting.Set<Supplier>().SingleAsync(s => s.IsActive));
            var restricted = false;
            try { await deleting.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number == 547) { restricted = true; }
            if (!restricted) throw new InvalidOperationException("Referenced supplier delete was not restricted.");
        }
        Console.WriteLine("PASS: migrated supplier relationship, eager report and conditional explicit loading");
    }
}
