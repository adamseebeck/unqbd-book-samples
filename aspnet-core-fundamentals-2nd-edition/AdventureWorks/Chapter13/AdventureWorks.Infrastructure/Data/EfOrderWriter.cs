using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// Stages order changes in the request's DbContext. EfUnitOfWork,
// which shares that context, saves them in one SaveChanges.
public sealed class EfOrderWriter(AdventureWorksDbContext db)
    : IOrderWriter
{
    // The next number after the newest order. Two requests can
    // pick the same one; the unique index refuses the second.
    public async Task<string> NextOrderNumberAsync(
        CancellationToken ct)
    {
        var last = await db.Orders.MaxAsync(o => (int?)o.Id, ct);
        return $"AW-{(last ?? 0) + 1:0000}";
    }

    public void Add(NewOrder order) => db.Orders.Add(new()
    {
        OwnerId = order.OwnerId,
        OrderNumber = order.OrderNumber,
        Items = [.. order.Items.Select(item => new OrderItemEntity
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPriceCents = Money.ToCents(item.UnitPrice)
        })]
    });

    public async Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct)
    {
        var row = await db.Orders.FindAsync([id], ct);
        if (row is null) return false;
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Status = status;
        return true;
    }

    // Chapter 10's notes update, staged. The client's version
    // goes in the UPDATE's WHERE clause at the save.
    public async Task<bool> SetNotesAsync(int id, string? notes,
        Guid version, CancellationToken ct)
    {
        var row = await db.Orders.FindAsync([id], ct);
        if (row is null) return false;
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Notes = notes;
        return true;
    }
}
