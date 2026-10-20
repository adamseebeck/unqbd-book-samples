using Microsoft.EntityFrameworkCore;
namespace AdventureWorks.DataSample;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public long UnitPriceCents { get; set; }
}
public sealed class OrderItem
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public long UnitPriceCents { get; set; }
}
public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid Version { get; set; } = Guid.NewGuid();
    public byte[]? RowVersion { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
public sealed class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Location { get; set; } = "Main";
    public int Quantity { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public sealed class OrderNote
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Text { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
}
public sealed record OrderReportDto(string OrderNumber, DateTime OrderDate, long TotalAmountCents);
public sealed record UpdateOrderRequest(string Notes, Guid Version);

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Product>().Property(p => p.Name).HasMaxLength(100).IsRequired();
        model.Entity<Product>().ToTable("Products", t => t.HasCheckConstraint("CK_Product_Price", "UnitPriceCents >= 0"));
        model.Entity<Order>().Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        model.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
        model.Entity<Order>().HasIndex(o => new { o.OwnerId, o.OrderDate });
        model.Entity<Order>().Property(o => o.Version).IsConcurrencyToken();
        if (Database.IsSqlServer()) model.Entity<Order>().Property(o => o.RowVersion).IsRowVersion();
        else model.Entity<Order>().Ignore(o => o.RowVersion);
        model.Entity<OrderItem>().ToTable("OrderItems", t => t.HasCheckConstraint("CK_Item_Quantity", "Quantity > 0"));
        model.Entity<OrderItem>().HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Order>().HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        model.Entity<InventoryItem>().HasKey(i => i.Id);
        model.Entity<InventoryItem>().Property(i => i.Location).HasMaxLength(100).IsRequired();
        model.Entity<InventoryItem>().HasIndex(i => new { i.ProductId, i.Location }).IsUnique();
        model.Entity<InventoryItem>().HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<InventoryItem>().Property(i => i.Version).IsConcurrencyToken();
        model.Entity<InventoryItem>().ToTable("InventoryItems", t => t.HasCheckConstraint("CK_Stock_Nonnegative", "Quantity >= 0"));
        model.Entity<OrderNote>().Property(n => n.Text).HasMaxLength(1000).IsRequired();
        model.Entity<OrderNote>().Property(n => n.Version).IsConcurrencyToken();
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RenewVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RenewVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    private void RenewVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Order>().Where(e => e.State == EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<InventoryItem>().Where(e => e.State == EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<OrderNote>().Where(e => e.State == EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
    }
}
