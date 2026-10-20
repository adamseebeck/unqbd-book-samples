using AdventureWorks.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

public sealed class AdventureWorksDbContext(DbContextOptions<AdventureWorksDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
public sealed class OrderRepository(AdventureWorksDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
public sealed class LocalInventory : IInventoryService
{
    public Task<bool> IsAvailableAsync(int productId, int quantity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(productId == 1 && quantity <= 10);
    }
}
public sealed class RejectAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
