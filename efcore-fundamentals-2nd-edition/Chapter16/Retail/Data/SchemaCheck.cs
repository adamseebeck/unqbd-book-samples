using Microsoft.EntityFrameworkCore;

public static class SchemaCheck
{
    public static async Task EnsureCurrentAsync(
        InventoryDbContext db)
    {
        var pending =
            await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
            throw new InvalidOperationException(
                "The database is missing migrations: " +
                string.Join(", ", pending) +
                ". Apply the reviewed migration script first.");
    }
}
