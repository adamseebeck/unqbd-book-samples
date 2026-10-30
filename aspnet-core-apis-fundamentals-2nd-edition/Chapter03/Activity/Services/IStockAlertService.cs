namespace WarehouseApi.Services;

public interface IStockAlertService
{
    Task SendAlertAsync(string message);
}
