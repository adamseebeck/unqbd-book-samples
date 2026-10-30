using Microsoft.EntityFrameworkCore;

public static class RetailDatabase
{
    public static async Task ResetAsync(
        InventoryDbContext db,
        Func<InventoryDbContext, Task>? seed = null)
    {
        var connection = db.Database.GetDbConnection();
        var safe = connection.DataSource.StartsWith(
                "(localdb)", StringComparison.OrdinalIgnoreCase)
            && connection.Database.StartsWith("EfCoreBook_");
        if (!safe)
            throw new InvalidOperationException(
                "Reset only a LocalDB EfCoreBook_ database.");

        var showSql = CommandLog.On;
        CommandLog.On = false;
        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            if (seed is not null) await seed(db);
            db.ChangeTracker.Clear();
        }
        finally
        {
            CommandLog.On = showSql;
            CommandLog.Count = 0;
        }
    }
}
