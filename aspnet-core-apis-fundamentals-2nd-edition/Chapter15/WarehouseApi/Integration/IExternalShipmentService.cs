namespace WarehouseApi.Integration;

public interface IExternalShipmentService
{
    Task<ShipmentConfirmation> CreateShipmentAsync(
        Guid orderId,
        string warehouseCode,
        CancellationToken cancellationToken = default);
}
