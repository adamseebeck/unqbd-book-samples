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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
    }
}
