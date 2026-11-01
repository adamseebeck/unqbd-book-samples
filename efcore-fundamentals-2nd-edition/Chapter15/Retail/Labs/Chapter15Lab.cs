using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

public static class Chapter15Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        var script = db.GetService<IMigrator>().GenerateScript(
            options: MigrationsSqlGenerationOptions.Idempotent);
        var batches = Regex
            .Split(script, @"^GO\s*$", RegexOptions.Multiline)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
        Console.WriteLine($"Script batches: {batches.Count}");

        var target = new SqlConnectionStringBuilder(
            db.Database.GetConnectionString())
        {
            InitialCatalog = "EfCoreBook_Deploy"
        };
        await using var deploy = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(target.ConnectionString)
                .Options);
        await deploy.Database.EnsureDeletedAsync();
        await deploy.GetService<IRelationalDatabaseCreator>()
            .CreateAsync();

        try
        {
            await SchemaCheck.EnsureCurrentAsync(deploy);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Empty database: check fails");
        }

        for (var run = 1; run <= 2; run++)
        {
            foreach (var batch in batches)
                await deploy.Database.ExecuteSqlRawAsync(batch);
            var applied = await deploy.Database
                .GetAppliedMigrationsAsync();
            var count = applied.Count();
            Console.WriteLine($"Run {run}: {count} migrations");
        }

        await SchemaCheck.EnsureCurrentAsync(deploy);
        Console.WriteLine("Deployed database: check passes");
    }
}
