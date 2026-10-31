using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed record CommandRecord(
    string Kind, string FirstLine, TimeSpan Duration);

public sealed class ReaderAudit : DbCommandInterceptor
{
    private readonly object gate = new();
    private readonly Queue<CommandRecord> records = new();
    private int count;
    private int nonQueryCount;

    public int Count => Volatile.Read(ref count);
    public int NonQueryCount => Volatile.Read(ref nonQueryCount);
    public TimeSpan LastDuration { get; private set; }
    public string LastFirstLine { get; private set; } = "";

    public static bool IsSlow(
        TimeSpan duration, TimeSpan threshold) =>
        duration >= threshold;

    public void Reset()
    {
        Interlocked.Exchange(ref count, 0);
        Interlocked.Exchange(ref nonQueryCount, 0);
        lock (gate) records.Clear();
    }

    public CommandRecord[] Records()
    {
        lock (gate) return records.ToArray();
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref count);
        LastDuration = eventData.Duration;
        LastFirstLine = FirstLine(command);
        Record("reader", command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref nonQueryCount);
        Record("non-query", command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    private void Record(
        string kind, DbCommand command, TimeSpan duration)
    {
        var record = new CommandRecord(
            kind, FirstLine(command), duration);
        lock (gate)
        {
            if (records.Count == 10) records.Dequeue();
            records.Enqueue(record);
        }
    }

    private static string FirstLine(DbCommand command) =>
        command.CommandText.Split('\n')[0].Trim();
}
