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


public sealed class ReaderAudit : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    private int count;
    public int Count => System.Threading.Volatile.Read(ref count);
    public void Reset() => System.Threading.Interlocked.Exchange(ref count, 0);
    public override ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(
        System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData,
        System.Data.Common.DbDataReader result, CancellationToken cancellationToken = default)
    {
        System.Threading.Interlocked.Increment(ref count);
        Console.WriteLine($"Reader command completed in {eventData.Duration.TotalMilliseconds:F2} ms");
        return ValueTask.FromResult(result);
    }
}

public sealed record CommandRecord(string Operation, TimeSpan Duration, string Kind, bool Slow);
public sealed class BoundedAudit : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    private readonly object gate = new();
    private readonly Queue<CommandRecord> records = new();
    private readonly TimeSpan threshold;
    public BoundedAudit(TimeSpan slowThreshold)
    {
        if (slowThreshold < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(slowThreshold));
        threshold = slowThreshold;
    }
    public static bool IsSlow(TimeSpan duration, TimeSpan threshold)
    {
        if (threshold < TimeSpan.Zero || duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(threshold));
        return duration >= threshold;
    }
    public CommandRecord[] Snapshot() { lock (gate) return records.ToArray(); }
    private void Record(TimeSpan duration, string kind)
    {
        lock (gate)
        {
            if (records.Count == 10) records.Dequeue();
            records.Enqueue(new CommandRecord("Inventory.LocalLab", duration, kind, IsSlow(duration, threshold)));
        }
    }
    public override ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData, System.Data.Common.DbDataReader result,
        CancellationToken cancellationToken = default)
    { Record(eventData.Duration, "reader"); return ValueTask.FromResult(result); }
    public override ValueTask<int> NonQueryExecutedAsync(System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    { Record(eventData.Duration, "nonquery"); return ValueTask.FromResult(result); }
}
public static class Program
{
    public static async Task Main()
    {
        if (BoundedAudit.IsSlow(TimeSpan.FromMilliseconds(9), TimeSpan.FromMilliseconds(10)) ||
            !BoundedAudit.IsSlow(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10)))
            throw new InvalidOperationException("Threshold boundary failed.");
        await using var lab = new LocalLab(16);
        await using (var setup = lab.Create()) { await setup.Database.EnsureCreatedAsync(); await Seed.ProductAsync(setup); }
        var audit = new BoundedAudit(TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<InventoryDbContext>(lab.Options).AddInterceptors(audit).Options;
        await using var db = new InventoryDbContext(options);
        _ = await db.Products.TagWith("Inventory.LowStock.v1").Select(p => p.Name).ToListAsync();
        _ = await db.Products.ExecuteUpdateAsync(set => set.SetProperty(p => p.StockQuantity, p => p.StockQuantity + 1));
        var records = audit.Snapshot();
        if (records.Count(x => x.Kind == "reader") != 1 || records.Count(x => x.Kind == "nonquery") != 1 ||
            records.Any(x => x.Operation.Contains("@") || !x.Slow)) throw new InvalidOperationException("Diagnostic boundary failed.");
        Console.WriteLine("PASS: deterministic threshold boundary, reader/nonquery events and non-private labels");
    }
}
