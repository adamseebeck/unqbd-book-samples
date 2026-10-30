using Microsoft.EntityFrameworkCore;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class StockHistoryService : IStockHistoryService
{
    private readonly WarehouseDbContext _context;

    public StockHistoryService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<StockAdjustmentDetails?> GetAdjustmentAsync(Guid id, CancellationToken cancellationToken = default) =>
        await (
                from adjustment in _context.StockAdjustments
                join stockItem in _context.StockItems on adjustment.StockItemId equals stockItem.Id
                where adjustment.Id == id
                select new StockAdjustmentDetails(
                    adjustment.Id, stockItem.WarehouseCode, stockItem.ProductSku, adjustment.ReasonCode,
                    adjustment.QuantityChange, adjustment.ReservedChange, adjustment.ReferenceNumber,
                    adjustment.CorrelationId, adjustment.AdjustedAt))
            .FirstOrDefaultAsync(cancellationToken);

    // Filter and sort before the projection: EF Core cannot translate members of the projected record
    public async Task<IReadOnlyList<StockAdjustmentDetails>> GetRecentAsync(string warehouseCode, int count, CancellationToken cancellationToken = default) =>
        await (
                from adjustment in _context.StockAdjustments
                join stockItem in _context.StockItems on adjustment.StockItemId equals stockItem.Id
                where stockItem.WarehouseCode == warehouseCode
                orderby adjustment.AdjustedAt descending, adjustment.Id
                select new StockAdjustmentDetails(
                    adjustment.Id, stockItem.WarehouseCode, stockItem.ProductSku, adjustment.ReasonCode,
                    adjustment.QuantityChange, adjustment.ReservedChange, adjustment.ReferenceNumber,
                    adjustment.CorrelationId, adjustment.AdjustedAt))
            .Take(count)
            .ToListAsync(cancellationToken);
}
