namespace WarehouseApi.Services;

public class StockAlertService : IStockAlertService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public StockAlertService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendAlertAsync(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var response = await _httpClient.PostAsJsonAsync("/alerts", new { Message = message });
        response.EnsureSuccessStatusCode();
    }

    // The factory registration creates the client for this service alone, so the service owns it
    // and disposes it; a client shared with other services would have to outlive them all
    public void Dispose()
    {
        _disposed = true;
        _httpClient.Dispose();
    }
}
