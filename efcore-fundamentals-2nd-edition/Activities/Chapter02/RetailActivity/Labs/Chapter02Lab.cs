using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class Chapter02Lab
{
    public static Task RunAsync(IServiceProvider services)
    {
        var environment = services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();

        var provider = db.Database.ProviderName;
        var database = db.Database.GetDbConnection().Database;
        var server = db.Database.GetDbConnection().DataSource;
        var retries = db.Database.CreateExecutionStrategy()
            .RetriesOnFailure;
        Console.WriteLine($"Environment: {environment}");
        Console.WriteLine($"Provider: {provider}");
        Console.WriteLine($"Database: {database}");
        Console.WriteLine($"Server: {server}");
        Console.WriteLine($"Retries on failure: {retries}");
        return Task.CompletedTask;
    }
}
