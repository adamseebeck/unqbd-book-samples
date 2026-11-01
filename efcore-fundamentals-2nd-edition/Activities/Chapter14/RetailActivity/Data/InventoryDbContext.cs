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
    public DbSet<Payment> Payments => Set<Payment>();

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
            product.Property(p => p.RowVersion).IsRowVersion();
            product.Property<DateTime>("LastModifiedUtc");
            product.HasQueryFilter(p => !p.IsDeleted);
            product.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Email).HasMaxLength(255);
            customer.HasIndex(c => c.Email).IsUnique();
            customer.Property(c => c.Status)
                .HasConversion<string>();
            customer.ComplexProperty(c => c.ShippingAddress,
                address =>
            {
                address.Property(a => a.Street).HasMaxLength(200);
                address.Property(a => a.PostalCode)
                    .HasMaxLength(20);
            });
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
            placement.HasQueryFilter(pc => !pc.Product.IsDeleted);
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

        model.Entity<Payment>(payment =>
        {
            payment.HasDiscriminator<string>("Kind")
                .HasValue<CardPayment>("card");
            payment.Property(p => p.Amount)
                .HasConversion(m => m.Value, v => new Money(v))
                .HasPrecision(18, 2);
        });
        model.Entity<CardPayment>()
            .Property(c => c.LastFour).HasMaxLength(4);
    }

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        StampAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditValues();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAuditValues()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Product>())
            if (entry.State is EntityState.Added
                or EntityState.Modified)
                entry.Property("LastModifiedUtc")
                    .CurrentValue = now;
    }
}
