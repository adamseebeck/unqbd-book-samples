using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// One SaveChanges for the request, translating the expected
// database failures into Core exceptions. Anything else is a real
// failure and stays an exception.
public sealed class EfUnitOfWork(AdventureWorksDbContext db)
    : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // No row left to compare with: deleted, not changed.
            foreach (var entry in ex.Entries)
            {
                var row = await entry.GetDatabaseValuesAsync(ct);
                if (row is null)
                    throw new OrderDeletedException(ex);
            }
            throw new OrderChangedException(ex);
        }
        catch (DbUpdateException ex)
            when (SqliteErrors.Is(ex, SqliteErrors.Unique) &&
                ex.InnerException!.Message.Contains(
                    "Orders.OrderNumber",
                    StringComparison.Ordinal))
        {
            throw new OrderNumberConflictException(ex);
        }
    }
}
