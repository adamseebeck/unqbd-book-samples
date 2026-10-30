using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class WarehouseService : IWarehouseService
{
    private readonly InMemoryWarehouseStore _store;

    public WarehouseService(InMemoryWarehouseStore store)
    {
        _store = store;
    }

    public Task<WarehouseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var warehouse = _store.Warehouses.Values.FirstOrDefault(w => w.Id == id);
            return Task.FromResult(warehouse?.ToResponse());
        }
    }

    public Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            IReadOnlyList<WarehouseResponse> warehouses = _store.Warehouses.Values
                .OrderBy(w => w.Id)
                .Select(w => w.ToResponse())
                .ToList();

            return Task.FromResult(warehouses);
        }
    }
}
