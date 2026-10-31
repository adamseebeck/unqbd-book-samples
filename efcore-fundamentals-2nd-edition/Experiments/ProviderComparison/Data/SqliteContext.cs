using Microsoft.EntityFrameworkCore;

public sealed class SqliteContext(
    DbContextOptions<SqliteContext> options)
    : DbContext(options)
{
    public DbSet<SqliteProduct> Products => Set<SqliteProduct>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SqliteProduct>(product =>
        {
            product.HasIndex(p => p.Sku).IsUnique();
            product.Property(p => p.Version)
                .IsConcurrencyToken();
            product.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId);
        });
    }

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        StampVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampVersions();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampVersions()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in
            ChangeTracker.Entries<SqliteProduct>())
            if (entry.State is EntityState.Added
                or EntityState.Modified)
                entry.Entity.Version = Guid.NewGuid();
    }
}
