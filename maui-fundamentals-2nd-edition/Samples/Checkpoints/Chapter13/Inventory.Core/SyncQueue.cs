using SQLite;
using System.Text;

namespace MauiInventory.Core;
public sealed class PendingSyncItem
{
    [PrimaryKey]
    public int ProductId { get; set; }
    public long Revision { get; set; }
    public string Payload { get; set; } = "";

    [Indexed]
    public int RetryCount { get; set; }
    public DateTimeOffset LastAttempt { get; set; }
    public DateTimeOffset NextAttempt { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public interface ISyncQueueStore
{
    Task<IReadOnlyList<PendingSyncItem>> ReadyAsync(DateTimeOffset now, CancellationToken token);
    Task AcknowledgeAsync(int productId, long revision, CancellationToken token);
    Task RecordFailureAsync(int productId, long revision, DateTimeOffset now, DateTimeOffset next, CancellationToken token);
}

public interface ISyncTransport
{
    Task SendAsync(PendingSyncItem item, CancellationToken token);
}

public sealed class SyncRevision
{
    [PrimaryKey]
    public int ProductId { get; set; }
    public long LastRevision { get; set; }
}

public sealed class SyncQueueProcessor(ISyncQueueStore queue, ISyncTransport transport, TimeProvider clock)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task ProcessAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            var now = clock.GetUtcNow();
            foreach (var item in await queue.ReadyAsync(now, token))
            {
                token.ThrowIfCancellationRequested();
                if (item.RetryCount >= 3 || item.NextAttempt > now)
                    continue;
                try
                {
                    await transport.SendAsync(item, token);
                    // This compare-and-delete must not remove a newer local revision.
                    await queue.AcknowledgeAsync(item.ProductId, item.Revision, token);
                }
                catch (HttpRequestException)
                {
                    var failedAt = clock.GetUtcNow();
                    await queue.RecordFailureAsync(item.ProductId, item.Revision, failedAt, failedAt.AddSeconds(Math.Pow(2, item.RetryCount)), token);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }
}

public sealed class SqliteSyncQueue : ISyncQueueStore, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection db;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private bool disposed;
    public SqliteSyncQueue(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("Use an absolute path.", nameof(path));
        db = new SQLiteAsyncConnection(path);
    }

    private async Task EnsureAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!initialized)
        {
            await db.CreateTableAsync<PendingSyncItem>();
            await db.CreateTableAsync<SyncRevision>();
            initialized = true;
        }
    }

    public async Task<long> EnqueueNextAsync(int productId, string payload, CancellationToken token = default)
    {
        if (productId <= 0 || Encoding.UTF8.GetByteCount(payload) > 65_536)
            throw new ArgumentException("Use a positive ID and at most 64 KiB.");
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            long revision = 0;
            await db.RunInTransactionAsync(connection =>
            {
                var counter = connection.Find<SyncRevision>(productId) ?? new SyncRevision
                {
                    ProductId = productId
                };
                var pending = connection.Find<PendingSyncItem>(productId);
                revision = checked(Math.Max(counter.LastRevision, pending?.Revision ?? 0) + 1);
                counter.LastRevision = revision;
                connection.InsertOrReplace(counter);
                connection.InsertOrReplace(new PendingSyncItem { ProductId = productId, Revision = revision, Payload = payload });
            });
            return revision;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task EnqueueAsync(PendingSyncItem item, CancellationToken token = default)
    {
        if (item.ProductId <= 0 || item.Revision <= 0 || Encoding.UTF8.GetByteCount(item.Payload) > 65_536)
            throw new ArgumentException("Use positive identifiers and a payload of at most 64 KiB.", nameof(item));
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            var existing = await db.FindAsync<PendingSyncItem>(item.ProductId);
            if (existing is not null)
            {
                if (item.Revision < existing.Revision)
                    throw new InvalidOperationException("A stale revision cannot replace newer queued work.");
                if (item.Revision == existing.Revision)
                {
                    if (item.Payload != existing.Payload)
                        throw new InvalidOperationException("The same revision cannot have two payloads.");
                    return; // Idempotent enqueue preserves retry/exhaustion state.
                }
            }

            await db.RunInTransactionAsync(connection =>
            {
                var counter = connection.Find<SyncRevision>(item.ProductId) ?? new SyncRevision
                {
                    ProductId = item.ProductId
                };
                if (item.Revision <= counter.LastRevision)
                    throw new InvalidOperationException("A previously consumed revision cannot be reused.");
                counter.LastRevision = item.Revision;
                connection.InsertOrReplace(counter);
                connection.InsertOrReplace(new PendingSyncItem { ProductId = item.ProductId, Revision = item.Revision, Payload = item.Payload });
            });
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<PendingSyncItem>> SnapshotAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            return await db.Table<PendingSyncItem>().OrderBy(x => x.ProductId).ToListAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<bool> RetryExhaustedAsync(int productId, long revision, CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            var row = await db.FindAsync<PendingSyncItem>(productId);
            if (row is null || row.Revision != revision || row.RetryCount < 3)
                return false;
            row.RetryCount = 0;
            row.NextAttempt = DateTimeOffset.MinValue;
            row.ErrorMessage = "";
            await db.UpdateAsync(row);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<int> PendingCountAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            return await db.Table<PendingSyncItem>().CountAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<PendingSyncItem>> ReadyAsync(DateTimeOffset now, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            return (await db.Table<PendingSyncItem>().Where(x => x.RetryCount < 3).ToListAsync()).Where(x => x.NextAttempt <= now).ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AcknowledgeAsync(int productId, long revision, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            await db.ExecuteAsync("DELETE FROM PendingSyncItem WHERE ProductId = ? AND Revision = ?", productId, revision);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RecordFailureAsync(int productId, long revision, DateTimeOffset now, DateTimeOffset next, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            var row = await db.FindAsync<PendingSyncItem>(productId);
            if (row is null || row.Revision != revision)
                return;
            row.RetryCount++;
            row.LastAttempt = now;
            row.NextAttempt = next;
            row.ErrorMessage = "Transient network failure";
            await db.UpdateAsync(row);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed)
                return;
            disposed = true;
            await db.CloseAsync();
        }
        finally
        {
            gate.Release();
        }
    }
}
