using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class StockAuditService : IStockAuditService
{
    private readonly WarehouseDbContext _context;

    public StockAuditService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockAdjustmentAuditItem>> GetHistoryAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.StockAdjustments.CountAsync(cancellationToken);

        // Newest first; the join adds the warehouse and product of each history row
        var items = await (
                from adjustment in _context.StockAdjustments
                join stockItem in _context.StockItems on adjustment.StockItemId equals stockItem.Id
                orderby adjustment.AdjustedAt descending, adjustment.Id
                select new StockAdjustmentAuditItem(
                    adjustment.Id,
                    stockItem.WarehouseCode,
                    stockItem.ProductSku,
                    adjustment.ReasonCode,
                    adjustment.QuantityChange,
                    adjustment.ReservedChange,
                    adjustment.ReferenceNumber,
                    adjustment.CorrelationId,
                    adjustment.AdjustedAt))
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockAdjustmentAuditItem>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }
}
