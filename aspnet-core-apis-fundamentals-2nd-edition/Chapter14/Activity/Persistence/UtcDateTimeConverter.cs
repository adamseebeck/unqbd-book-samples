using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WarehouseApi.Persistence;

// The API stores only UTC times, so a DateTime read from SQL Server is marked as UTC again
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
