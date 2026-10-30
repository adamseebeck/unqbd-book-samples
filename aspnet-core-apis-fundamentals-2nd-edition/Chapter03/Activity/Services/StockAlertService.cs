namespace WarehouseApi.Services;

public class StockAlertService : IStockAlertService, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _lock = new();

    public StockAlertService(string filePath)
    {
        // The file stays open until the container disposes the service
        _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
    }

    public Task SendAlertAsync(string message)
    {
        // One Singleton serves every request, so concurrent alerts take turns writing
        lock (_lock)
        {
            _writer.WriteLine($"{DateTimeOffset.UtcNow:O} {message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
