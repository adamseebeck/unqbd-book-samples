using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class ReaderAudit : DbCommandInterceptor
{
    private int count;

    public int Count => Volatile.Read(ref count);
    public TimeSpan LastDuration { get; private set; }
    public string LastFirstLine { get; private set; } = "";

    public void Reset() => Interlocked.Exchange(ref count, 0);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref count);
        LastDuration = eventData.Duration;
        LastFirstLine = command.CommandText.Split('\n')[0].Trim();
        return ValueTask.FromResult(result);
    }
}
