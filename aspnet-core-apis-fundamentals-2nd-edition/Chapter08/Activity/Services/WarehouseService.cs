using System.Collections.Concurrent;
using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class WarehouseService : IWarehouseService
{
    // The two sample warehouses; the API does not create warehouses (Chapter 10 seeds them in the database)
    private readonly ConcurrentDictionary<int, WarehouseResponse> _warehouses = new()
    {
        [1] = new WarehouseResponse(1, "WH-001", "North Distribution Center", "Chicago, IL", true),
        [2] = new WarehouseResponse(2, "WH-002", "South Fulfillment Hub", "Atlanta, GA", true)
    };

    public Task<WarehouseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _warehouses.TryGetValue(id, out var warehouse);
        return Task.FromResult(warehouse);
    }

    public Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<WarehouseResponse>>(_warehouses.Values.OrderBy(w => w.Id).ToList());
    }
}
