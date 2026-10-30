using Microsoft.Data.SqlClient;

namespace WarehouseApi.Tests.Infrastructure;

public static class TestDatabase
{
    // LocalDB by default; a CI job sets WAREHOUSE_TEST_SQLSERVER to its SQL Server instead
    private const string DefaultServer =
        "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true";

    // A new database name for every call, so test classes never share rows
    public static string CreateConnectionString()
    {
        var server = Environment.GetEnvironmentVariable("WAREHOUSE_TEST_SQLSERVER") ?? DefaultServer;
        var builder = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"WarehouseApiTests_{Guid.NewGuid():N}"
        };

        return builder.ConnectionString;
    }

    // EnsureDeletedAsync clears every connection pool in the process, which breaks the test classes that
    // still run in parallel. This drops one database and clears only that database's pool
    public static async Task DropAsync(string connectionString)
    {
        await using (var connection = new SqlConnection(connectionString))
        {
            SqlConnection.ClearPool(connection);
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];";
        await command.ExecuteNonQueryAsync();
    }
}
