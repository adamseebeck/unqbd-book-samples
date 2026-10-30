using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class EfWarehouseService : IWarehouseService
{
    private readonly WarehouseDbContext _context;

    public EfWarehouseService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return warehouse?.ToResponse();
    }

    public async Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Id)
            .ToListAsync(cancellationToken);

        return warehouses.Select(w => w.ToResponse()).ToList();
    }
}
