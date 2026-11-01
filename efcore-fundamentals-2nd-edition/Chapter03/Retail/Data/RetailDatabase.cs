using Microsoft.EntityFrameworkCore;

public static class RetailDatabase
{
    public static async Task ResetAsync(InventoryDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var safe = connection.DataSource.StartsWith(
                "(localdb)", StringComparison.OrdinalIgnoreCase)
            && connection.Database.StartsWith("EfCoreBook_");
        if (!safe)
            throw new InvalidOperationException(
                "Reset only a LocalDB EfCoreBook_ database.");

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
