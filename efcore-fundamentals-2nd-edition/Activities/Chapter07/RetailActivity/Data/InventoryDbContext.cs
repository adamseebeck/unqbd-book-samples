using Microsoft.EntityFrameworkCore;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductCategory> ProductCategories
        => Set<ProductCategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(100);
            category.HasIndex(c => c.Name).IsUnique();
        });

        model.Entity<Product>(product =>
        {
            product.ToTable("Products");
            product.Property(p => p.Name).HasMaxLength(200);
            product.Property(p => p.Sku).HasMaxLength(40);
            product.HasIndex(p => p.Sku).IsUnique();
            product.Property(p => p.Price).HasPrecision(18, 2);
            product.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            product.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Email).HasMaxLength(255);
            customer.HasIndex(c => c.Email).IsUnique();
            customer.Property(c => c.Status)
                .HasConversion<string>();
        });

        model.Entity<Order>(order =>
        {
            order.Property(o => o.Status).HasConversion<string>();
            order.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<OrderItem>(item =>
        {
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
            item.HasOne(i => i.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            item.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<ProductCategory>(placement =>
        {
            placement.HasKey(pc =>
                new { pc.ProductId, pc.CategoryId });
            placement.HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            placement.HasOne(pc => pc.Category)
                .WithMany()
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CustomerProfile>(profile =>
        {
            profile.HasKey(cp => cp.CustomerId);
            profile.HasOne(cp => cp.Customer)
                .WithOne(c => c.Profile)
                .HasForeignKey<CustomerProfile>(
                    cp => cp.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
