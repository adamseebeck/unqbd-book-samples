using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WarehouseApi.Persistence;

public static class SqlServerErrors
{
    // 2601 and 2627: a row with the same unique index or primary key value already exists
    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
