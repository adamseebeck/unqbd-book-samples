using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter01Lab
{
    public static Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();

        var threshold = 5;
        var query = db.Products
            .Where(p => p.StockQuantity < threshold)
            .OrderBy(p => p.Id);
        Console.WriteLine(query.ToQueryString());
        return Task.CompletedTask;
    }
}
