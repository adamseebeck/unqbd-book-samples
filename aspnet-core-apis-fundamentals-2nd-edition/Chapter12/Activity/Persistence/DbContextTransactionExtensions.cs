using System.Data;
using Microsoft.EntityFrameworkCore;

namespace WarehouseApi.Persistence;

public static class DbContextTransactionExtensions
{
    // Runs the operation as one retriable unit: after a transient failure the execution
    // strategy starts the whole transaction again, not just the statement that failed
    public static Task<T> RunInTransactionAsync<T>(
        this DbContext context,
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async ct =>
        {
            // Every attempt starts with an empty change tracker and reads current values
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, ct);
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }
}
