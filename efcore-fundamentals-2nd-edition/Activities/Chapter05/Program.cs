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
        b.Ignore(x => x.Products);
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



public sealed record RenameSupplier(int Id, string CompanyName);
public static class SupplierWrites
{
    public static async Task RenameAsync(InventoryDbContext db, RenameSupplier input)
    {
        if (string.IsNullOrWhiteSpace(input.CompanyName) || input.CompanyName.Length > 150)
            throw new ArgumentException("Invalid company name.");
        var row = await db.Set<Supplier>().SingleAsync(x => x.Id == input.Id);
        row.CompanyName = input.CompanyName.Trim();
        await db.SaveChangesAsync();
    }
}
public static class Program
{
    public static async Task Main()
    {
        await using var lab = new LocalLab(5);
        int id;
        await using (var db = lab.Create())
        {
            await db.Database.EnsureCreatedAsync();
            var row = new Supplier { CompanyName = "Original", ContactEmail = "supplier@example.test", CreatedAt = DateTime.UtcNow };
            db.Add(row); await db.SaveChangesAsync(); id = row.Id;
        }
        await using (var edit = lab.Create()) await SupplierWrites.RenameAsync(edit, new(id, "Renamed"));
        await using (var duplicate = lab.Create())
        {
            duplicate.Add(new Supplier { CompanyName = "Duplicate", ContactEmail = "supplier@example.test", CreatedAt = DateTime.UtcNow });
            var rejected = false;
            try { await duplicate.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Duplicate supplier email accepted.");
        }
        await using (var read = lab.Create())
        {
            var row = await read.Set<Supplier>().SingleAsync();
            if (row.CompanyName != "Renamed" || row.ContactEmail != "supplier@example.test") throw new InvalidOperationException("Allowlist failed.");
            var invalid = false;
            try { await SupplierWrites.RenameAsync(read, new(id, " ")); } catch (ArgumentException) { invalid = true; }
            if (!invalid) throw new InvalidOperationException("Invalid company name accepted.");
            read.Remove(row); await read.SaveChangesAsync();
        }
        await using var verify = lab.Create();
        if (await verify.Set<Supplier>().AnyAsync()) throw new InvalidOperationException("Delete did not persist.");
        Console.WriteLine("PASS: supplier insert, allowlist update, validation and delete");
    }
}
