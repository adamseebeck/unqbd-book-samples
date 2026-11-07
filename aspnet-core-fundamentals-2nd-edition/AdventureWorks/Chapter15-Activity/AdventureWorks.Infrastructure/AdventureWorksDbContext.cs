using AdventureWorks.Infrastructure.Data;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure;

// One context for the whole app. Chapter 8 stores Identity users
// and refresh tokens; Chapter 10 adds the catalog and orders.
public sealed class AdventureWorksDbContext(
    DbContextOptions<AdventureWorksDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<InventoryItemEntity> InventoryItems =>
        Set<InventoryItemEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
        builder.ApplyConfigurationsFromAssembly(
            typeof(AdventureWorksDbContext).Assembly);
    }

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        RenewVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RenewVersions();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess, cancellationToken);
    }

    // A changed order gets a new Version. The UPDATE still checks
    // the old one, so a stale writer changes no row.
    void RenewVersions()
    {
        var changed = ChangeTracker.Entries<OrderEntity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var order in changed)
            order.Entity.Version = Guid.NewGuid();
    }
}
