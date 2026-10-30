using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ContosoSupportCopilot.Infrastructure.Data;

// EnsureCreated creates every table of a new database but skips a database that already has tables,
// so a table added in a later chapter never reaches a database that an earlier chapter created.
// This update creates only the model's missing tables and their indexes, with the SQL the database
// provider generates for them. It never changes or drops an existing table; a schema that keeps
// changing needs EF Core migrations.
public static class DatabaseSchemaUpdater
{
    public static async Task<IReadOnlyList<string>> CreateMissingTablesAsync(this DbContext context, CancellationToken cancellationToken = default)
    {
        var existing = await ListTablesAsync(context, cancellationToken);
        var (tables, commands) = PlanMissingTables(context, existing);
        foreach (var command in commands)
        {
            await context.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }

        return tables;
    }

    // The provider's SQL for the tables that the list does not contain; needs no open connection.
    public static (IReadOnlyList<string> Tables, IReadOnlyList<string> Commands) PlanMissingTables(
        DbContext context, IReadOnlyCollection<string> existingTables)
    {
        var existing = existingTables.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var model = context.GetService<IDesignTimeModel>().Model;

        // The operations that would create the whole model, filtered to what is missing.
        var operations = context.GetService<IMigrationsModelDiffer>()
            .GetDifferences(null, model.GetRelationalModel())
            .Where(operation => operation switch
            {
                CreateTableOperation table => !existing.Contains(table.Name),
                CreateIndexOperation index => !existing.Contains(index.Table),
                _ => false
            })
            .ToList();

        var commands = context.GetService<IMigrationsSqlGenerator>().Generate(operations, model)
            .Select(command => command.CommandText)
            .ToList();
        return (operations.OfType<CreateTableOperation>().Select(table => table.Name).ToList(), commands);
    }

    private static async Task<List<string>> ListTablesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var sql = context.Database.IsSqlite()
            ? "SELECT name AS Value FROM sqlite_master WHERE type = 'table'"
            : "SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        return await context.Database.SqlQueryRaw<string>(sql).ToListAsync(cancellationToken);
    }
}
