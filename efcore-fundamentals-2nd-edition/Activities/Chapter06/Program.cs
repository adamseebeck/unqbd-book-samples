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



public sealed record LowStockRow(int Id, string Name, string Category, int StockQuantity);
public static class Program
{
    private static IOrderedQueryable<Product> Query(InventoryDbContext db, int threshold, int pageSize)
    {
        if (threshold < 0 || pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(threshold));
        return db.Products.AsNoTracking().Where(p => p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity).ThenBy(p => p.Id);
    }
    public static async Task Main()
    {
        await using var lab = new LocalLab(6);
        await using var db = lab.Create();
        await db.Database.EnsureCreatedAsync();
        var category = new Category { Name = "Books" };
        for (var i = 0; i < 5; i++) db.Products.Add(new Product { Name = $"Item {i}", Sku = $"ITEM-{i}", Category = category, StockQuantity = i / 2 });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var query = Query(db, 10, 2);
        var invalidInputs = 0;
        try { _ = Query(db, -1, 2); } catch (ArgumentOutOfRangeException) { invalidInputs++; }
        try { _ = Query(db, 10, -1); } catch (ArgumentOutOfRangeException) { invalidInputs++; }
        if (invalidInputs != 2) throw new InvalidOperationException("Invalid report input accepted.");
        var offset = new List<int>(); var seek = new List<int>();
        var printedOffset = false; var printedSeek = false;
        for (var page = 0; ; page++)
        {
            var offsetQuery = query.Skip(page * 2).Take(2)
                .Select(p => new LowStockRow(p.Id, p.Name, p.Category.Name, p.StockQuantity));
            if (page == 1)
            {
                Console.WriteLine("OFFSET SQL:"); Console.WriteLine(offsetQuery.ToQueryString()); printedOffset = true;
            }
            var rows = await offsetQuery.ToListAsync();
            if (rows.Count == 0) break;
            offset.AddRange(rows.Select(x => x.Id));
        }
        var lastStock = -1; var lastId = 0;
        while (true)
        {
            var seekQuery = query.Where(p => p.StockQuantity > lastStock ||
                (p.StockQuantity == lastStock && p.Id > lastId)).Take(2)
                .Select(p => new LowStockRow(p.Id, p.Name, p.Category.Name, p.StockQuantity));
            if (lastId > 0 && !printedSeek)
            {
                Console.WriteLine("SEEK SQL:"); Console.WriteLine(seekQuery.ToQueryString()); printedSeek = true;
            }
            var rows = await seekQuery.ToListAsync();
            if (rows.Count == 0) break;
            seek.AddRange(rows.Select(x => x.Id)); lastStock = rows[^1].StockQuantity; lastId = rows[^1].Id;
        }
        if (!printedOffset || !printedSeek || offset.Count != 5 || offset.Distinct().Count() != 5 || !offset.SequenceEqual(seek))
            throw new InvalidOperationException("Pagination repeated or skipped a row.");
        Console.WriteLine("PASS: stable offset and seek pages cover five rows once");
    }
}
