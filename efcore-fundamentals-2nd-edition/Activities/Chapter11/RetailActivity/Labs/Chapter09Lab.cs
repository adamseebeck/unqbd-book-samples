using Microsoft.Extensions.DependencyInjection;

public static class Chapter09Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
            await RetailDatabase.ResetAsync(
                scope.GetContext(), Seed.CatalogAsync);

        using var request = services.CreateScope();
        var reader = request.ServiceProvider
            .GetRequiredService<IInventoryReader>();
        var rows = await reader.LowStockAsync(threshold: 6);
        foreach (var row in rows)
            Console.WriteLine(
                $"Low stock: {row.Name} ({row.StockQuantity})");

        var tracked = request.GetContext()
            .ChangeTracker.Entries().Count();
        Console.WriteLine($"Tracked entities: {tracked}");
    }
}
