public sealed class InventoryWorker(IServiceScopeFactory scopes)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var reader = scope.ServiceProvider
            .GetRequiredService<IInventoryReader>();
        var rows = await reader.LowStockAsync(6);
        Console.WriteLine(
            $"Worker: {rows.Count} low-stock products");
    }
}
