using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

public sealed class EfOrderRepository(AdventureWorksDbContext db)
    : IOrderRepository
{
    public const int PageSize = 10;

    public async Task<Order?> GetByIdAsync(int id)
    {
        var row = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id);
        return row is null ? null : ToOrder(row);
    }

    // One bounded page, newest first. SQL sums the cents.
    public async Task<OrderSummary[]> ListAsync(
        string ownerId, int page)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.Status, o.Archived,
                Cents = o.Items.Sum(i =>
                    i.Quantity * i.UnitPriceCents)
            })
            .ToArrayAsync();
        return [.. rows.Select(r => new OrderSummary(r.Id,
            r.OrderNumber, Money.FromCents(r.Cents), r.Status,
            r.Archived))];
    }

    public async Task<UpdateOutcome> UpdateNotesAsync(
        int id, string? notes, Guid version)
    {
        var row = await db.Orders.FindAsync(id);
        if (row is null) return UpdateOutcome.NotFound;
        // Compare with the version the client read, not the one
        // just loaded: the UPDATE runs WHERE Version = version.
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Notes = notes;
        try
        {
            await db.SaveChangesAsync();
            return UpdateOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            // No row matched: the order changed, or was deleted.
            return await db.Orders.AnyAsync(o => o.Id == id)
                ? UpdateOutcome.Conflict : UpdateOutcome.NotFound;
        }
    }

    // ExecuteUpdate bypasses SaveChanges, so it sets the new
    // Version itself.
    public async Task<bool> ArchiveAsync(int id)
    {
        var rows = await db.Orders.Where(o => o.Id == id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(o => o.Archived, true)
                .SetProperty(o => o.Version, Guid.NewGuid()));
        return rows == 1;
    }

    // The database deletes the order's items with it (cascade).
    public async Task<bool> DeleteAsync(int id) =>
        await db.Orders.Where(o => o.Id == id)
            .ExecuteDeleteAsync() == 1;

    static Order ToOrder(OrderEntity row)
    {
        OrderItem[] items = [.. row.Items.OrderBy(i => i.Id)
            .Select(i => new OrderItem(i.ProductId, i.Quantity,
                Money.FromCents(i.UnitPriceCents)))];
        return new Order(row.Id, row.OrderNumber, row.OwnerId,
            items.Sum(i => i.Quantity * i.UnitPrice), row.Status,
            row.Archived, row.Notes, row.Version, items);
    }
}
