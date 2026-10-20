using Microsoft.EntityFrameworkCore;
namespace AdventureWorks.DataSample;

public sealed class OrderQueries(ApplicationDbContext db)
{
    public async Task<List<OrderReportDto>> ReportAsync(string ownerId, int page, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("An authenticated owner is required.");
        if (page is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(page));
        return await db.Orders.AsNoTracking().Where(o => o.OwnerId == ownerId)
            .OrderByDescending(o => o.OrderDate).ThenBy(o => o.Id)
            .Skip((page - 1) * 20).Take(20)
            .Select(o => new OrderReportDto(o.OrderNumber, o.OrderDate,
                o.Items.Sum(i => (long?)i.Quantity * i.UnitPriceCents) ?? 0)).ToListAsync(ct);
    }
    public Task<Order?> DetailsAsync(string ownerId, Guid id, CancellationToken ct)
        => db.Orders.AsNoTracking().Where(o => o.OwnerId == ownerId && o.Id == id)
            .Include(o => o.Items).ThenInclude(i => i.Product).SingleOrDefaultAsync(ct);

    public async Task<bool> TransferAsync(int sourceId, int destinationId, int quantity, CancellationToken ct)
    {
        if (sourceId == destinationId || quantity <= 0) throw new ArgumentException("Use different inventory positions and positive quantity.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var source = await db.InventoryItems.SingleOrDefaultAsync(i => i.Id == sourceId, ct);
        var target = await db.InventoryItems.SingleOrDefaultAsync(i => i.Id == destinationId, ct);
        if (source is null || target is null || source.ProductId != target.ProductId || source.Quantity < quantity) return false;
        source.Quantity -= quantity;
        await db.SaveChangesAsync(ct);
        target.Quantity = checked(target.Quantity + quantity);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
    public async Task<bool> AddNoteAsync(string ownerId, Guid orderId, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000) throw new ArgumentException("Use 1-1000 characters.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId && o.OwnerId == ownerId, ct);
        if (order is null) return false;
        order.Notes = "A note was added.";
        // Every note participates in the parent version check, even when Notes is unchanged.
        db.Entry(order).Property(o => o.Version).IsModified = true;
        db.OrderNotes.Add(new OrderNote { OrderId = orderId, Text = text });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return false; }
        await transaction.CommitAsync(ct);
        return true;
    }
}
