using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter04Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();

        var pending =
            await db.Database.GetPendingMigrationsAsync();
        Console.WriteLine($"Pending: {Describe(pending)}");
        await db.Database.MigrateAsync();
        var applied =
            await db.Database.GetAppliedMigrationsAsync();
        Console.WriteLine($"Applied: {Describe(applied)}");
    }

    private static string Describe(IEnumerable<string> ids) =>
        ids.Any() ? string.Join(", ", ids) : "(none)";
}
