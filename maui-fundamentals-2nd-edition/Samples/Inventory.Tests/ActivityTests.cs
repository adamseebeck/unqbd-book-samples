using MauiInventory.Core;
using SQLite;
using System.Net;
using Xunit;

namespace Inventory.Tests;
public sealed class ActivityTests
{
    [Fact]
    public async Task Detail_replacement_waits_for_old_load_and_displays_new_identifier()
    {
        var repository = new DetailRepository();
        var vm = new ProductDetailViewModel(repository);
        using var departure = new CancellationTokenSource();
        var old = vm.InitializeAsync(1, departure.Token);
        await repository.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        departure.Cancel();
        var latest = vm.InitializeAsync(2);
        repository.Release.TrySetResult(true);
        await Task.WhenAll(old, latest).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { 1, 2 }, repository.Ids);
        Assert.Equal(2, vm.Product!.Id);
        Assert.Empty(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task Refresh_while_replacement_waits_uses_the_latest_selected_identifier()
    {
        var repository = new DetailRepository();
        var vm = new ProductDetailViewModel(repository);
        var old = vm.InitializeAsync(1);
        await repository.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var replacement = vm.InitializeAsync(2);
        var refresh = vm.LoadProductCommand.ExecuteAsync(null);
        repository.Release.TrySetResult(true);
        await Task.WhenAll(old, replacement, refresh).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { 1, 2 }, repository.Ids);
        Assert.Equal(2, vm.Product!.Id);
    }

    [Fact]
    public async Task Detail_rejects_invalid_and_reports_missing_without_a_raw_error()
    {
        var vm = new ProductDetailViewModel(new MemoryProductRepository());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => vm.InitializeAsync(0));
        await vm.InitializeAsync(999);
        Assert.Null(vm.Product);
        Assert.Equal("Product not found.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Legacy_schema_upgrade_preserves_rows_and_sets_version_once()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "legacy.db3");
        await LegacyAsync(path, false);
        await using (var repository = new SqliteProductRepository(path))
        {
            var row = Assert.Single(await repository.ListAsync());
            Assert.Equal("Legacy", row.Name);
            Assert.Equal(125, row.UnitPriceCents);
        }

        var db = new SQLiteAsyncConnection(path);
        try
        {
            Assert.Equal(1, await db.ExecuteScalarAsync<int>("PRAGMA user_version"));
            Assert.Contains(await db.GetTableInfoAsync("Product"), c => c.Name == "UpdatedUtc");
        }
        finally
        {
            await db.CloseAsync();
        }
    }

    [Fact]
    public async Task Failed_upgrade_rolls_back_column_version_and_preserves_duplicate_rows()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "legacy.db3");
        await LegacyAsync(path, true);
        await using (var repository = new SqliteProductRepository(path))
        {
            await Assert.ThrowsAsync<SQLiteException>(() => repository.ListAsync());
        }

        var db = new SQLiteAsyncConnection(path);
        try
        {
            Assert.Equal(0, await db.ExecuteScalarAsync<int>("PRAGMA user_version"));
            Assert.DoesNotContain(await db.GetTableInfoAsync("Product"), c => c.Name == "UpdatedUtc");
            Assert.Equal(2, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Product"));
        }
        finally
        {
            await db.CloseAsync();
        }
    }

    [Fact]
    public async Task Future_schema_is_rejected_without_modifying_its_data_or_version()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "future.db3");
        var db = new SQLiteAsyncConnection(path);
        await db.ExecuteAsync("CREATE TABLE FutureData (Value TEXT)");
        await db.ExecuteAsync("INSERT INTO FutureData VALUES ('preserve')");
        await db.ExecuteAsync("PRAGMA user_version=42");
        await db.CloseAsync();
        await using (var repository = new SqliteProductRepository(path))
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => repository.ListAsync());
        }

        db = new SQLiteAsyncConnection(path);
        try
        {
            Assert.Equal(42, await db.ExecuteScalarAsync<int>("PRAGMA user_version"));
            Assert.Equal("preserve", await db.ExecuteScalarAsync<string>("SELECT Value FROM FutureData"));
            Assert.Empty(await db.GetTableInfoAsync("Product"));
        }
        finally
        {
            await db.CloseAsync();
        }
    }

    [Fact]
    public async Task Category_description_survives_reopen_and_failed_export_does_not_set_success()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "categories.db3");
        await using (var repository = new CategoryRepository(path))
        {
            await repository.AddAsync(new Category { Name = "Office", Description = "Desks and chairs" });
        }

        await using (var repository = new CategoryRepository(path))
        {
            var settings = new ExportSettings();
            var files = new ExportFiles
            {
                Fail = true
            };
            var vm = new CategoryViewModel(repository, files, settings);
            await vm.LoadCommand.ExecuteAsync(null);
            Assert.Equal("Desks and chairs", Assert.Single(vm.Categories).Description);
            await vm.ExportCommand.ExecuteAsync(null);
            Assert.Null(vm.LastExportUtc);
            Assert.Equal(0, settings.Writes);
            Assert.NotEmpty(vm.ErrorMessage);
            files.Fail = false;
            await vm.ExportCommand.ExecuteAsync(null);
            Assert.NotNull(vm.LastExportUtc);
            Assert.Equal(1, settings.Writes);
            Assert.Contains("Desks and chairs", files.Contents);
        }
    }

    [Fact]
    public async Task Queue_rejects_late_stale_enqueue_and_equal_revision_cannot_reset_failures()
    {
        using var folder = new PrivateFolder();
        await using var queue = new SqliteSyncQueue(Path.Combine(folder.Path, "queue.db3"));
        await queue.EnqueueAsync(new() { ProductId = 1, Revision = 2, Payload = "current" });
        await queue.RecordFailureAsync(1, 2, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => queue.EnqueueAsync(new() { ProductId = 1, Revision = 1, Payload = "old" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => queue.EnqueueAsync(new() { ProductId = 1, Revision = 2, Payload = "conflicting" }));
        await queue.EnqueueAsync(new() { ProductId = 1, Revision = 2, Payload = "current" });
        var row = Assert.Single(await queue.SnapshotAsync());
        Assert.Equal(2, row.Revision);
        Assert.Equal(1, row.RetryCount);
    }

    [Fact]
    public async Task Allocated_queue_revisions_remain_monotonic_after_acknowledgment_and_restart()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "queue.db3");
        long first;
        await using (var queue = new SqliteSyncQueue(path))
        {
            first = await queue.EnqueueNextAsync(1, "first");
            await queue.AcknowledgeAsync(1, first, default);
        }

        await using (var queue = new SqliteSyncQueue(path))
        {
            var second = await queue.EnqueueNextAsync(1, "second");
            Assert.True(second > first);
            await queue.AcknowledgeAsync(1, first, default);
            Assert.Equal(second, Assert.Single(await queue.SnapshotAsync()).Revision);
        }
    }

    [Fact]
    public async Task Queue_backoff_starts_at_failure_completion_not_at_batch_start()
    {
        using var folder = new PrivateFolder();
        await using var queue = new SqliteSyncQueue(Path.Combine(folder.Path, "queue.db3"));
        await queue.EnqueueNextAsync(1, "current");
        var clock = new Clock();
        var processor = new SyncQueueProcessor(queue, new SlowFailure(clock), clock);
        await processor.ProcessAsync();
        var row = Assert.Single(await queue.SnapshotAsync());
        Assert.Equal(clock.Now, row.LastAttempt);
        Assert.Equal(clock.Now.AddSeconds(1), row.NextAttempt);
        Assert.Empty(await queue.ReadyAsync(clock.Now, default));
    }

    [Fact]
    public async Task Queue_exhaustion_survives_restart_and_explicit_recovery_compares_revision()
    {
        using var folder = new PrivateFolder();
        var path = Path.Combine(folder.Path, "queue.db3");
        var now = DateTimeOffset.UtcNow;
        await using (var queue = new SqliteSyncQueue(path))
        {
            await queue.EnqueueAsync(new() { ProductId = 1, Revision = 3, Payload = "current" });
            for (int i = 0; i < 3; i++)
                await queue.RecordFailureAsync(1, 3, now, now, default);
        }

        await using (var queue = new SqliteSyncQueue(path))
        {
            Assert.Empty(await queue.ReadyAsync(now.AddDays(1), default));
            Assert.Equal(3, Assert.Single(await queue.SnapshotAsync()).RetryCount);
            Assert.False(await queue.RetryExhaustedAsync(1, 2));
            Assert.True(await queue.RetryExhaustedAsync(1, 3));
            Assert.Equal(0, Assert.Single(await queue.ReadyAsync(now, default)).RetryCount);
        }
    }

    [Fact]
    public async Task Duplicate_local_order_attempts_share_one_save_and_failure_never_navigates()
    {
        var orders = new GatedOrders();
        var navigation = new Navigation();
        var vm = new OrderViewModel(orders, navigation)
        {
            IsValid = true,
            Total = 10
        };
        var first = vm.SubmitOrderAsync(default);
        await orders.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.SubmitOrderAsync(default);
        Assert.Equal(1, orders.Calls);
        orders.Release.TrySetResult(true);
        await first;
        Assert.Equal(1, navigation.Calls);
        orders.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => vm.SubmitOrderAsync(default));
        Assert.Equal(1, navigation.Calls);
    }

    [Fact]
    public void Shake_requires_two_peaks_and_obeys_window_debounce_and_reset()
    {
        var detector = new ShakeDetector();
        Assert.False(detector.Observe(0, 0, 1, TimeSpan.Zero));
        Assert.False(detector.Observe(0, 0, 3, TimeSpan.FromMilliseconds(10)));
        Assert.True(detector.Observe(0, 0, 3, TimeSpan.FromMilliseconds(510)));
        Assert.False(detector.Observe(0, 0, 4, TimeSpan.FromMilliseconds(520)));
        Assert.False(detector.Observe(0, 0, 3, TimeSpan.FromMilliseconds(2510)));
        Assert.False(detector.Observe(0, 0, 3, TimeSpan.FromMilliseconds(3011)));
        Assert.True(detector.Observe(0, 0, 3, TimeSpan.FromMilliseconds(3020)));
        detector.Reset();
        Assert.False(detector.Observe(0, 0, 3, TimeSpan.Zero));
    }

    [Fact]
    public async Task Refresh_overlap_is_real_and_cancellation_failure_and_expiry_are_not_cached()
    {
        var clock = new Clock();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        var coordinator = new AccessTokenCoordinator(async token =>
        {
            Interlocked.Increment(ref calls);
            started.TrySetResult(true);
            await release.Task.WaitAsync(token);
            return new("fresh", clock.Now.AddHours(1));
        }, clock);
        var first = coordinator.GetAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = coordinator.GetAsync();
        Assert.False(second.IsCompleted);
        release.TrySetResult(true);
        await Task.WhenAll(first, second);
        Assert.Equal(1, calls);
        var failed = new AccessTokenCoordinator(_ => throw new IOException("safe"), clock);
        await Assert.ThrowsAsync<IOException>(() => failed.GetAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed.GetAsync());
        var expired = new AccessTokenCoordinator(_ => Task.FromResult(new AccessSession("old", clock.Now)), clock);
        await Assert.ThrowsAsync<InvalidDataException>(() => expired.GetAsync());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.GetAsync(canceled.Token));
    }

    [Fact]
    public async Task Thumbnail_cache_is_dimension_specific_lru_bounded_and_does_not_expose_stored_arrays()
    {
        int decodes = 0;
        var cache = new ThumbnailCache(4);
        Task<byte[]> Decode(ThumbnailKey key, CancellationToken token)
        {
            decodes++;
            return Task.FromResult(new byte[] { (byte)key.WidthPixels, 2 });
        }

        var a = new ThumbnailKey("asset", 1, 1);
        var b = new ThumbnailKey("asset", 2, 1);
        var c = new ThumbnailKey("asset", 3, 1);
        var result = await cache.GetAsync(a, Decode);
        result[0] = 99;
        Assert.Equal(1, (await cache.GetAsync(a, Decode))[0]);
        await cache.GetAsync(b, Decode);
        await cache.GetAsync(a, Decode);
        await cache.GetAsync(c, Decode);
        await cache.GetAsync(b, Decode);
        Assert.Equal(4, decodes);
    }

    [Fact]
    public async Task Bounded_metrics_drop_newest_when_full_and_drain_without_external_delivery()
    {
        var buffer = new LocalMetricBuffer();
        for (int i = 0; i < 64; i++)
            Assert.True(buffer.TryRecord(new(OperationCategory.PageLoad, i, true)));
        Assert.False(buffer.TryRecord(new(OperationCategory.PageLoad, 64, true)));
        buffer.Complete();
        var count = 0;
        await foreach (var metric in buffer.ReadAllAsync())
        {
            Assert.Equal(count++, metric.ElapsedMilliseconds);
        }

        Assert.Equal(64, count);
    }

    [Fact]
    public void Active_message_subscription_attaches_once_detaches_and_can_return()
    {
        var publisher = new Messages();
        int calls = 0;
        using var subscription = new ActiveMessageSubscription((_, _) => calls++);
        subscription.Attach(publisher);
        subscription.Attach(publisher);
        publisher.Publish();
        Assert.Equal(1, calls);
        subscription.Dispose();
        publisher.Publish();
        Assert.Equal(1, calls);
        subscription.Attach(publisher);
        publisher.Publish();
        Assert.Equal(2, calls);
    }

    private static async Task LegacyAsync(string path, bool duplicates)
    {
        var db = new SQLiteAsyncConnection(path);
        try
        {
            await db.ExecuteAsync("CREATE TABLE Product (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sku TEXT, Name TEXT, Quantity INTEGER, UnitPriceCents INTEGER)");
            await db.ExecuteAsync("INSERT INTO Product (Sku,Name,Quantity,UnitPriceCents) VALUES ('OLD','Legacy',2,125)");
            if (duplicates)
                await db.ExecuteAsync("INSERT INTO Product (Sku,Name,Quantity,UnitPriceCents) VALUES ('OLD','Duplicate',1,50)");
        }
        finally
        {
            await db.CloseAsync();
        }
    }

    private sealed class DetailRepository : IProductRepository
    {
        public List<int> Ids = [];
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Product?> GetAsync(int id, CancellationToken token = default)
        {
            Ids.Add(id);
            if (id == 1)
            {
                Started.TrySetResult(true);
                await Release.Task;
            }

            token.ThrowIfCancellationRequested();
            return new Product
            {
                Id = id,
                Sku = "SKU" + id,
                Name = "Product" + id
            };
        }

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task SaveAsync(Product product, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class ExportSettings : IExportSettings
    {
        public int Writes;
        public void SetLastExport(DateTimeOffset value) => Writes++;
    }

    private sealed class ExportFiles : ITextFileStore
    {
        public bool Fail;
        public string Contents = "";
        public Task WriteAsync(string name, string text, CancellationToken token)
        {
            if (Fail)
                throw new IOException();
            Contents = text;
            return Task.CompletedTask;
        }
    }

    private sealed class GatedOrders : IOrderRepository
    {
        public int Calls;
        public bool Fail;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<int> SaveAsync(decimal total, CancellationToken token)
        {
            Calls++;
            if (Fail)
                throw new IOException();
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(token);
            return 42;
        }
    }

    private sealed class Navigation : INavigationService
    {
        public int Calls;
        public Task ShowConfirmationAsync(int id, CancellationToken token)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class SlowFailure(Clock clock) : ISyncTransport
    {
        public Task SendAsync(PendingSyncItem item, CancellationToken token)
        {
            clock.Now = clock.Now.AddSeconds(10);
            throw new HttpRequestException();
        }
    }

    private sealed class Messages : IMessagePublisher
    {
        public event EventHandler? Updated;
        public void Publish() => Updated?.Invoke(this, EventArgs.Empty);
    }

    private sealed class PrivateFolder : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("maui-activity-");
        public string Path => directory.FullName;

        public void Dispose() => directory.Delete(true);
    }
}
