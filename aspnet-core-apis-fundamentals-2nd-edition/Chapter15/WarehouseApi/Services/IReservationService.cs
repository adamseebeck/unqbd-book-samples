using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IReservationService
{
    Task<OrderResponse> ReserveAsync(ReservationRequest request, CancellationToken cancellationToken = default);
}
