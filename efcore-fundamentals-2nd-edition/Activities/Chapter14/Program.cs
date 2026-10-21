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
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductCategory> ProductCategories { get; set; } = [];
    public byte[] RowVersion { get; set; } = [];
    public bool IsDeleted { get; set; }
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
    public Address ShippingAddress { get; set; } = new();
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
public sealed class Address
{
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
}
public readonly record struct Money(decimal Value);
public abstract class Payment
{
    public int Id { get; set; }
    public Money Amount { get; set; }
}
public sealed class CardPayment : Payment
{
    public string LastFour { get; set; } = "";
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
        model.Entity<Product>().ToTable("Products");
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

        model.Entity<Product>().Property(x => x.RowVersion).IsRowVersion();
        model.Entity<Customer>().OwnsOne(x => x.ShippingAddress, b =>
        {
            b.Property(x => x.Street).HasMaxLength(200);
            b.Property(x => x.PostalCode).HasMaxLength(20);
        });
        model.Entity<Payment>().HasDiscriminator<string>("Kind").HasValue<CardPayment>("card");
        model.Entity<Payment>().Property(x => x.Amount)
            .HasConversion(x => x.Value, x => new Money(x)).HasPrecision(18, 2);
        model.Entity<CardPayment>().Property(x => x.LastFour).HasMaxLength(4);
        model.Entity<Product>().Property<DateTime>("LastModifiedUtc");
        model.Entity<Product>().HasQueryFilter(x => !x.IsDeleted);
    }
    private void StampAuditValues()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Product>())
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Property("LastModifiedUtc").CurrentValue = now;
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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



public static class Program
{
    public static async Task Main()
    {
        await using var lab = new LocalLab(14);
        int id;
        DateTime syncBefore, syncAfter, asyncBefore, asyncAfter;
        await using (var db = lab.Create())
        {
            await db.Database.EnsureCreatedAsync(); var p = await Seed.ProductAsync(db); id = p.Id;
            p.IsDeleted = true; syncBefore = DateTime.UtcNow; db.SaveChanges(); syncAfter = DateTime.UtcNow;
        }
        await using (var persisted = lab.Create())
        {
            var saved = await persisted.Products.IgnoreQueryFilters().Where(p => p.Id == id)
                .Select(p => new { p.IsDeleted, Utc = EF.Property<DateTime>(p, "LastModifiedUtc") }).SingleAsync();
            if (!saved.IsDeleted || saved.Utc < syncBefore || saved.Utc > syncAfter)
                throw new InvalidOperationException("Sync save did not persist the UTC interval.");
            Console.WriteLine($"Persisted sync audit: {saved.Utc:O}; interval: {syncBefore:O}..{syncAfter:O}");
        }
        await using (var restore = lab.Create())
        {
            if (await restore.Products.AnyAsync()) throw new InvalidOperationException("Deleted product remained visible.");
            var p = await restore.Products.IgnoreQueryFilters().SingleAsync(); p.IsDeleted = false;
            asyncBefore = DateTime.UtcNow;
            await restore.SaveChangesAsync(false, CancellationToken.None);
            asyncAfter = DateTime.UtcNow;
            if (restore.Entry(p).State != EntityState.Modified) throw new InvalidOperationException("State accepted too early.");
            restore.ChangeTracker.AcceptAllChanges();
            if (restore.Entry(p).State != EntityState.Unchanged) throw new InvalidOperationException("State not accepted.");
        }
        await using (var persisted = lab.Create())
        {
            var saved = await persisted.Products.Where(p => p.Id == id)
                .Select(p => new { p.IsDeleted, Utc = EF.Property<DateTime>(p, "LastModifiedUtc") }).SingleAsync();
            if (saved.IsDeleted || saved.Utc < asyncBefore || saved.Utc > asyncAfter)
                throw new InvalidOperationException("Restore did not persist its UTC interval and normal visibility.");
            Console.WriteLine($"Persisted async restore audit: {saved.Utc:O}; interval: {asyncBefore:O}..{asyncAfter:O}");
        }
        var stamped = DateTime.UtcNow;
        await using (var bulk = lab.Create())
        {
            var changed = await bulk.Products.Where(p => p.Id == id).ExecuteUpdateAsync(set => set
                .SetProperty(p => p.IsDeleted, true).SetProperty(p => EF.Property<DateTime>(p, "LastModifiedUtc"), stamped));
            if (changed != 1) throw new InvalidOperationException("Bulk retirement affected wrong rows.");
        }
        await using var read = lab.Create();
        var stored = await read.Products.IgnoreQueryFilters().Select(p => EF.Property<DateTime>(p, "LastModifiedUtc")).SingleAsync();
        if (stored != stamped || await read.Products.AnyAsync()) throw new InvalidOperationException("Bulk audit/filter mismatch.");
        Console.WriteLine("PASS: persisted sync/async UTC intervals, false acceptance, restored visibility and separate bulk audit");
    }
}
