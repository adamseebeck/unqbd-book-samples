using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

public sealed class EfInventoryRepository(
    AdventureWorksDbContext db) : IInventoryRepository
{
    public Task<StockLevel[]> GetAsync(int productId) =>
        db.InventoryItems.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Location)
            .Select(s => new StockLevel(s.Location, s.Quantity))
            .ToArrayAsync();

    // Two saves, one transaction. The second save fails when the
    // source would go negative; the rollback undoes the first.
    public async Task<TransferOutcome> TransferAsync(
        StockTransfer transfer)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync();
        var from = await FindAsync(transfer.ProductId,
            transfer.From);
        var to = await FindAsync(transfer.ProductId, transfer.To);
        if (from is null || to is null)
            return TransferOutcome.NotFound;
        to.Quantity += transfer.Quantity;
        await db.SaveChangesAsync();
        from.Quantity -= transfer.Quantity;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (StoreErrors.IsCheckFailure(ex))
        {
            // CK_InventoryItems_Quantity refused it. Leaving
            // without a commit rolls the transaction back.
            return TransferOutcome.NotEnoughStock;
        }
        await transaction.CommitAsync();
        return TransferOutcome.Moved;
    }

    Task<InventoryItemEntity?> FindAsync(
        int productId, string location) =>
        db.InventoryItems.SingleOrDefaultAsync(s =>
            s.ProductId == productId && s.Location == location);
}
