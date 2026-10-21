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

public static class Transfer
{
    public static async Task RunAsync(string connection, int sourceId, int targetId, int quantity,
        CancellationToken cancellationToken = default)
    {
        if (sourceId == targetId || quantity <= 0) throw new ArgumentException("Invalid transfer.");
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connection,
            sql => sql.EnableRetryOnFailure(3)).Options;
        await using var strategyContext = new InventoryDbContext(options);
        await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = new InventoryDbContext(options);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var source = await db.Products.SingleAsync(p => p.Id == sourceId, cancellationToken);
            var target = await db.Products.SingleAsync(p => p.Id == targetId, cancellationToken);
            if (source.StockQuantity < quantity) throw new InvalidOperationException("Insufficient stock.");
            source.StockQuantity -= quantity;
            await db.SaveChangesAsync(cancellationToken);
            target.StockQuantity = checked(target.StockQuantity + quantity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}

public static class StockAdjustment
{
    public static async Task SaveAsync(InventoryDbContext db, Product product, int delta,
        CancellationToken ct = default)
    {
        if (delta == 0) throw new ArgumentOutOfRangeException(nameof(delta));
        product.StockQuantity = checked(product.StockQuantity + delta);
        if (product.StockQuantity < 0) throw new InvalidOperationException("Insufficient stock.");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try { await db.SaveChangesAsync(ct); return; }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
                var entry = db.Entry(product);
                var current = await entry.GetDatabaseValuesAsync(ct)
                    ?? throw new InvalidOperationException("Product was deleted.");
                entry.CurrentValues.SetValues(current);
                entry.OriginalValues.SetValues(current);
                entry.State = EntityState.Unchanged;
                var adjusted = checked(product.StockQuantity + delta);
                if (adjusted < 0) throw new InvalidOperationException("Insufficient stock.");
                product.StockQuantity = adjusted;
                db.ChangeTracker.DetectChanges();
                Console.WriteLine($"Retry {attempt + 1}: Price.IsModified={entry.Property(p => p.Price).IsModified}");
            }
        }
    }
}
public static class OrderService
{
    public static async Task<int> PlaceAsync(Func<InventoryDbContext> create, int customerId,
        int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        await using var db = create();
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct)) throw new InvalidOperationException("Unknown customer.");
        var product = await db.Products.SingleAsync(p => p.Id == productId, ct);
        if (product.StockQuantity < quantity) throw new InvalidOperationException("Insufficient stock.");
        product.StockQuantity -= quantity;
        var order = new Order { CustomerId = customerId, OrderDate = DateTime.UtcNow, Status = OrderStatus.Pending,
            OrderItems = [new OrderItem { ProductId = productId, Quantity = quantity, UnitPrice = product.Price }] };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return order.Id;
    }
}

public sealed class InjectedFailure : Exception { }
public static class StockService
{
    public static async Task AdjustAsync(Func<InventoryDbContext> create, int id, int adjustment,
        Func<Task>? firstRead = null, CancellationToken ct = default)
    {
        if (adjustment == 0) throw new ArgumentOutOfRangeException(nameof(adjustment));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var db = create();
            var product = await db.Products.SingleAsync(p => p.Id == id, ct);
            var next = checked(product.StockQuantity + adjustment);
            if (next < 0) throw new InvalidOperationException("Insufficient stock.");
            product.StockQuantity = next;
            if (attempt == 0 && firstRead is not null) await firstRead();
            try { await db.SaveChangesAsync(ct); return; }
            catch (DbUpdateConcurrencyException) when (attempt < 2) { await Task.Delay(10 * (attempt + 1), ct); }
        }
    }
    public static async Task TransferAsync(Func<InventoryDbContext> create, int from, int to, int quantity, bool fail)
    {
        if (quantity <= 0 || from == to) throw new ArgumentException("Invalid transfer.");
        await using var db = create();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
        var source = await db.Products.SingleAsync(p => p.Id == from);
        var target = await db.Products.SingleAsync(p => p.Id == to);
        if (source.StockQuantity < quantity) throw new InvalidOperationException("Insufficient stock.");
        source.StockQuantity -= quantity; await db.SaveChangesAsync();
        if (fail) throw new InjectedFailure();
        target.StockQuantity = checked(target.StockQuantity + quantity); await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
public static class Program
{
    public static async Task Main()
    {
        await using var lab = new LocalLab(8);
        int sourceId, targetId;
        await using (var setup = lab.Create())
        {
            await setup.Database.EnsureCreatedAsync(); var p = await Seed.ProductAsync(setup); sourceId = p.Id;
            var target = new Product { Name = "Target", Sku = "TARGET", CategoryId = p.CategoryId, StockQuantity = 4 };
            setup.Add(target); await setup.SaveChangesAsync(); targetId = target.Id;
        }
        await StockService.AdjustAsync(lab.Create, sourceId, -3, async () =>
        {
            await using var competing = lab.Create(); var p = await competing.Products.SingleAsync(p => p.Id == sourceId);
            p.StockQuantity -= 2; await competing.SaveChangesAsync();
        });
        var injected = false;
        try { await StockService.TransferAsync(lab.Create, sourceId, targetId, 2, true); } catch (InjectedFailure) { injected = true; }
        await using (var check = lab.Create())
        {
            if (!injected || (await check.Products.SingleAsync(p => p.Id == sourceId)).StockQuantity != 5 ||
                (await check.Products.SingleAsync(p => p.Id == targetId)).StockQuantity != 4)
                throw new InvalidOperationException("Conflict recomputation or rollback failed.");
        }
        await StockService.TransferAsync(lab.Create, sourceId, targetId, 2, false);
        await using var read = lab.Create();
        if ((await read.Products.SingleAsync(p => p.Id == sourceId)).StockQuantity != 3 ||
            (await read.Products.SingleAsync(p => p.Id == targetId)).StockQuantity != 6)
            throw new InvalidOperationException("Transfer did not preserve total stock.");
        Console.WriteLine("PASS: deterministic concurrency retry, partial-save rollback and committed transfer");
    }
}
