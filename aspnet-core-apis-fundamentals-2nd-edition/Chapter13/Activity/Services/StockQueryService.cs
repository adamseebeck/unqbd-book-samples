using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Persistence;
using WarehouseApi.Telemetry;

namespace WarehouseApi.Services;

public class StockQueryService : IStockQueryService
{
    private readonly WarehouseDbContext _context;

    public StockQueryService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockListItem>> QueryAsync(StockQueryFilter filter, CancellationToken cancellationToken = default)
    {
        using var activity = WarehouseTelemetry.Source.StartActivity(WarehouseTelemetry.ActivityNames.QueryStock);

        // The query ends in a projection of plain values, which EF Core never tracks,
        // so AsNoTracking is not needed here; it matters for queries that return entities
        var query = _context.StockItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Sku))
        {
            query = query.Where(s => s.ProductSku == filter.Sku);
        }

        if (!string.IsNullOrWhiteSpace(filter.WarehouseCode))
        {
            query = query.Where(s => s.WarehouseCode == filter.WarehouseCode);
        }

        if (filter.BelowReorderThreshold == true)
        {
            query = query.Where(s => s.OnHand <= s.ReorderThreshold);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // The projection asks SQL Server for these columns only; it joins Products for the name
        var items = await query
            .OrderBy(s => s.ProductSku)
            .ThenBy(s => s.WarehouseCode)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new StockListItem(
                s.ProductSku,
                s.Product.Name,
                s.WarehouseCode,
                s.OnHand,
                s.Reserved,
                s.OnHand - s.Reserved,
                s.ReorderThreshold))
            .ToListAsync(cancellationToken);

        activity?.SetTag(WarehouseTelemetry.Tags.ResultCount, items.Count);

        return new PagedResult<StockListItem>(items, totalCount, filter.PageNumber, filter.PageSize);
    }
}
