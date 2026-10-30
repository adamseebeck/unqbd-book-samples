namespace WarehouseApi.Services;

// Scoped: one instance per request, disposed by the container when the request ends
public sealed class InventoryRequestLog : IDisposable
{
    private readonly ILogger<InventoryRequestLog> _logger;
    private int _lookups;

    public InventoryRequestLog(ILogger<InventoryRequestLog> logger)
    {
        _logger = logger;
    }

    public void RecordLookup() => Interlocked.Increment(ref _lookups);

    public void Dispose()
    {
        _logger.LogInformation("Request scope ended after {Lookups} stock lookup(s)", _lookups);
    }
}
