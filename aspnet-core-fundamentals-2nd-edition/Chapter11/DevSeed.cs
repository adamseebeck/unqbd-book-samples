using Microsoft.EntityFrameworkCore;
namespace AdventureWorks.DataSample;

// Development-only sample data for the requests in requests.http. It never migrates: run
// dotnet ef database update first. It seeds only an empty database and changes nothing otherwise.
public static class DevSeed
{
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.GetMigrations().Any() || (await db.Database.GetPendingMigrationsAsync()).Any())
        {
            app.Logger.LogWarning("DevSeed skipped: add and apply the migration first (dotnet ef migrations add InitialCreate, then dotnet ef database update).");
            return;
        }
        if (await db.Products.AnyAsync()) return;
        // One known product: an order line stores this server-side price, whatever the request says.
        db.Products.Add(new Product { Name = "Notebook", UnitPriceCents = 1200 }); // id 1 in the new, empty database
        await db.SaveChangesAsync();
        app.Logger.LogInformation("DevSeed added product 1 (Notebook, 1200 cents).");
    }
}
