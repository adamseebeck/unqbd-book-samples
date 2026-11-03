using Microsoft.Data.Sqlite;

namespace AdventureWorks.Infrastructure.Data;

// SQLite's extended result codes for the constraints the store
// relies on (https://sqlite.org/rescode.html).
internal static class SqliteErrors
{
    public const int Check = 275;
    public const int Unique = 2067;
    // ON DELETE RESTRICT runs as an internal trigger, so a
    // refused delete reports SQLITE_CONSTRAINT_TRIGGER.
    public const int RestrictedDelete = 1811;

    // SaveChanges wraps the provider's exception; ExecuteUpdate
    // and ExecuteDelete throw it directly.
    public static bool Is(Exception exception, int code) =>
        (exception as SqliteException
            ?? exception.InnerException as SqliteException)
        ?.SqliteExtendedErrorCode == code;
}
