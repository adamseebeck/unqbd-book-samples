using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IWarehouseService
{
    Task<WarehouseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
