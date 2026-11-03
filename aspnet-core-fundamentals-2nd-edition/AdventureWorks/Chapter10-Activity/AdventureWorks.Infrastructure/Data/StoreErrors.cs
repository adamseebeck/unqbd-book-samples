using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace AdventureWorks.Infrastructure.Data;

// The constraint errors the store relies on, for both providers.
// SQLite: extended result codes (sqlite.org/rescode.html).
// SQL Server: error numbers 2601 and 2627 (duplicate key) and 547
// (a CHECK or FOREIGN KEY constraint refused the statement).
internal static class StoreErrors
{
    public static bool IsDuplicateKey(Exception exception) =>
        Sqlite(exception) == 2067
        || SqlServer(exception) is 2601 or 2627;

    public static bool IsCheckFailure(Exception exception) =>
        Sqlite(exception) == 275 || SqlServer(exception) == 547;

    // SQLite reports ON DELETE RESTRICT as a trigger error, 1811.
    public static bool IsRestrictedDelete(Exception exception) =>
        Sqlite(exception) == 1811 || SqlServer(exception) == 547;

    // SaveChanges wraps the provider's exception; ExecuteUpdate
    // and ExecuteDelete throw it directly.
    static int? Sqlite(Exception exception) =>
        (exception as SqliteException
            ?? exception.InnerException as SqliteException)
        ?.SqliteExtendedErrorCode;

    static int? SqlServer(Exception exception) =>
        (exception as SqlException
            ?? exception.InnerException as SqlException)?.Number;
}
