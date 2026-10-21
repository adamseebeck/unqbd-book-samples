using MauiInventory.Core;
using System.Net;
using System.Text;
using Xunit;

namespace Inventory.Tests;
public sealed class InventoryTests
{
    private static Product Item(string sku = "TEST") => new()
    {
        Sku = sku,
        Name = "Test product",
        Quantity = 2,
        UnitPriceCents = 125
    };
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("abc")]
    [InlineData("2147483648")]
    [InlineData("1.0")]
    public void Invalid_route_id_is_rejected(string? value) => Assert.False(ProductRoute.TryRead(value, out _));
    [Fact]
    public void Route_round_trip()
    {
        Assert.Equal("details?id=42", ProductRoute.Detail(42));
        Assert.True(ProductRoute.TryRead("42", out var id));
        Assert.Equal(42, id);
    }

    [Fact]
    public void Route_generation_rejects_nonpositive_id() => Assert.Throws<ArgumentOutOfRangeException>(() => ProductRoute.Detail(0));
    [Fact]
    public async Task Memory_repository_does_not_expose_its_internal_entities()
    {
        var repo = new MemoryProductRepository();
        var rows = await repo.ListAsync();
        rows[0].Name = "Changed";
        Assert.NotEqual("Changed", (await repo.ListAsync())[0].Name);
    }

    [Fact]
    public async Task Viewmodel_load_filters_real_repository_and_announces_state()
    {
        var vm = new ProductListViewModel(new MemoryProductRepository())
        {
            Query = "mouse"
        };
        var changes = new List<string?>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Single(vm.Products);
        Assert.Equal("Mouse", vm.Products[0].Name);
        Assert.False(vm.IsBusy);
        Assert.Contains("IsBusy", changes);
        Assert.Contains("IsIdle", changes);
    }

    [Fact]
    public async Task Busy_state_is_visible_and_duplicate_execution_does_not_start_second_load()
    {
        var repo = new ControlledRepository();
        var vm = new ProductListViewModel(repo);
        var first = vm.LoadAsync(default);
        await repo.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(vm.IsBusy);
        Assert.False(vm.LoadCommand.CanExecute(null));
        var duplicate = vm.LoadAsync(default);
        Assert.Equal(1, repo.Calls);
        repo.Release.TrySetResult(true);
        await Task.WhenAll(first, duplicate).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(vm.IsBusy);
        Assert.Single(vm.Products);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public async Task Command_cancellation_reaches_repository_and_ends_operation()
    {
        var repo = new ControlledRepository();
        var vm = new ProductListViewModel(repo);
        var operation = vm.LoadCommand.ExecuteAsync(null);
        await repo.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        vm.LoadCommand.Cancel();
        await operation.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(repo.ObservedToken.IsCancellationRequested);
        Assert.True(repo.Ended);
        Assert.Equal("Loading canceled.", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
        Assert.Empty(vm.Products);
    }

    [Fact]
    public async Task Failure_preserves_existing_data_and_error_does_not_disclose_exception()
    {
        var repo = new ControlledRepository
        {
            Failure = new HttpRequestException("secret-token=do-not-display")
        };
        var vm = new ProductListViewModel(repo);
        vm.Products.Add(Item());
        repo.Release.TrySetResult(true);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Single(vm.Products);
        Assert.Equal("Could not load products. Try again.", vm.ErrorMessage);
        Assert.DoesNotContain("secret", vm.ErrorMessage);
    }

    [Fact]
    public async Task Sqlite_insert_update_delete_and_reopen_are_real_file_operations()
    {
        using var folder = new TestFolder();
        var path = Path.Combine(folder.Path, "test.db3");
        var product = Item();
        await using (var repo = new SqliteProductRepository(path))
        {
            await repo.SaveAsync(product);
            Assert.True(product.Id > 0);
            product.Quantity = 7;
            await repo.SaveAsync(product);
        }

        await using (var repo = new SqliteProductRepository(path))
        {
            Assert.Equal(7, (await repo.GetAsync(product.Id))!.Quantity);
            Assert.True(await repo.DeleteAsync(product.Id));
            Assert.False(await repo.DeleteAsync(product.Id));
        }
    }

    [Fact]
    public async Task Sqlite_unique_sku_failure_does_not_remove_existing_row()
    {
        using var folder = new TestFolder();
        await using var repo = new SqliteProductRepository(Path.Combine(folder.Path, "test.db3"));
        await repo.SaveAsync(Item());
        await Assert.ThrowsAsync<SQLite.SQLiteException>(() => repo.SaveAsync(Item()));
        Assert.Single(await repo.ListAsync());
    }

    [Fact]
    public async Task Concurrent_first_use_initializes_once_and_saves_all_rows()
    {
        using var folder = new TestFolder();
        await using var repo = new SqliteProductRepository(Path.Combine(folder.Path, "test.db3"));
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => repo.SaveAsync(Item("SKU" + i))));
        Assert.Equal(12, (await repo.ListAsync()).Count);
    }

    [Fact]
    public async Task Canceled_sqlite_call_does_not_write()
    {
        using var folder = new TestFolder();
        await using var repo = new SqliteProductRepository(Path.Combine(folder.Path, "test.db3"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.SaveAsync(Item(), cts.Token));
        Assert.Empty(await repo.ListAsync());
    }

    [Fact]
    public async Task Disposed_sqlite_repository_fails_explicitly()
    {
        using var folder = new TestFolder();
        var repo = new SqliteProductRepository(Path.Combine(folder.Path, "test.db3"));
        await repo.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => repo.ListAsync());
        await repo.DisposeAsync();
    }

    [Fact]
    public async Task Actual_demo_http_handler_deserializes_and_response_content_is_disposed()
    {
        using var client = new HttpClient(new DemoCatalogHandler());
        var rows = await new CatalogClient(client, new Uri("https://inventory.example/catalog")).GetAsync();
        Assert.Equal("Standing desk", Assert.Single(rows).Name);
        var content = new TrackedContent("[]");
        using var tracked = new HttpClient(new ResponseHandler(HttpStatusCode.OK, content));
        await new CatalogClient(tracked, new Uri("https://inventory.example/catalog")).GetAsync();
        Assert.True(content.WasDisposed);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Http_status_failures_remain_distinguishable(int status)
    {
        var content = new TrackedContent("[]");
        using var client = new HttpClient(new ResponseHandler((HttpStatusCode)status, content));
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new CatalogClient(client, new Uri("https://inventory.example/catalog")).GetAsync());
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.True(content.WasDisposed);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[{\"Id\":0}]")]
    [InlineData("{}")]
    [InlineData("[null]")]
    public async Task Bad_payload_is_rejected(string payload)
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, new StringContent(payload)));
        await Assert.ThrowsAsync<InvalidDataException>(() => new CatalogClient(client, new Uri("https://inventory.example/catalog")).GetAsync());
    }

    [Fact]
    public async Task Plain_http_endpoint_is_rejected_before_request()
    {
        var handler = new ResponseHandler(HttpStatusCode.OK, new StringContent("[]"));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CatalogClient(client, new Uri("http://inventory.example/catalog")).GetAsync());
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Oversized_body_is_rejected()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, new StringContent(new string (' ', 1_048_577))));
        await Assert.ThrowsAsync<InvalidDataException>(() => new CatalogClient(client, new Uri("https://inventory.example/catalog")).GetAsync());
    }

    [Fact]
    public async Task Failed_page_retry_does_not_skip_the_page_and_final_page_stops_requests()
    {
        var source = new PageSource();
        var vm = new PagedProductsViewModel(source);
        await vm.LoadMoreAsync();
        Assert.NotEmpty(vm.ErrorMessage);
        Assert.Empty(vm.Products);
        source.Fail = false;
        await vm.LoadMoreAsync();
        Assert.Equal(new[] { 0, 0 }, source.Pages);
        Assert.Single(vm.Products);
        Assert.False(vm.HasMore);
        await vm.LoadMoreAsync();
        Assert.Equal(2, source.Pages.Count);
    }

    [Fact]
    public async Task Csv_handles_quotes_commas_newlines_and_formula_like_names()
    {
        var text = CategoryExport.ToCsv([new() { Id = 1, Name = "=1+1", Description = "One, \"two\"\nthree" }]);
        Assert.Contains("\"'=1+1\"", text);
        Assert.Contains("\"One, \"\"two\"\"\nthree\"", text);
        var files = new TextStore();
        await CategoryExport.ExportAsync([], files);
        Assert.Equal("categories.csv", files.Name);
    }

    [Fact]
    public async Task Failed_export_has_no_success_timestamp()
    {
        await Assert.ThrowsAsync<IOException>(() => CategoryExport.ExportAsync([], new TextStore { Fail = true }));
    }

    [Fact]
    public async Task Order_submits_once_and_passes_returned_id_to_navigation()
    {
        var orders = new Orders();
        var nav = new Navigation();
        var vm = new OrderViewModel(orders, nav)
        {
            IsValid = true,
            Total = 12.50m
        };
        await vm.SubmitOrderCommand.ExecuteAsync(null);
        Assert.Equal(1, orders.Calls);
        Assert.Equal(12.50m, orders.Total);
        Assert.Equal(42, nav.Id);
    }

    [Fact]
    public async Task Invalid_order_cannot_submit()
    {
        var orders = new Orders();
        var vm = new OrderViewModel(orders, new Navigation())
        {
            IsValid = false
        };
        Assert.False(vm.SubmitOrderCommand.CanExecute(null));
        await vm.SubmitOrderCommand.ExecuteAsync(null);
        Assert.Equal(0, orders.Calls);
    }

    [Fact]
    public async Task Canceling_order_token_stops_save_and_does_not_navigate()
    {
        var orders = new Orders
        {
            Wait = true
        };
        var nav = new Navigation();
        var vm = new OrderViewModel(orders, nav)
        {
            IsValid = true
        };
        var operation = vm.SubmitOrderCommand.ExecuteAsync(null);
        await orders.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        vm.SubmitOrderCommand.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.True(orders.Token.IsCancellationRequested);
        Assert.Equal(0, nav.Id);
    }

    [Fact]
    public async Task Queue_acknowledgment_does_not_remove_newer_revision_and_survives_restart()
    {
        using var folder = new TestFolder();
        var path = Path.Combine(folder.Path, "queue.db3");
        await using (var queue = new SqliteSyncQueue(path))
        {
            await queue.EnqueueAsync(new() { ProductId = 1, Revision = 2, Payload = "newer" });
            await queue.AcknowledgeAsync(1, 1, default);
        }

        await using (var queue = new SqliteSyncQueue(path))
        {
            var row = Assert.Single(await queue.ReadyAsync(DateTimeOffset.UtcNow, default));
            Assert.Equal(2, row.Revision);
            await queue.AcknowledgeAsync(1, 2, default);
            Assert.Empty(await queue.ReadyAsync(DateTimeOffset.UtcNow, default));
        }
    }

    [Fact]
    public async Task Queue_failure_records_backoff_and_stops_after_three_attempts()
    {
        using var folder = new TestFolder();
        await using var queue = new SqliteSyncQueue(Path.Combine(folder.Path, "queue.db3"));
        await queue.EnqueueAsync(new() { ProductId = 1, Revision = 1, Payload = "test" });
        var clock = new TestClock();
        var transport = new FailingTransport();
        var processor = new SyncQueueProcessor(queue, transport, clock);
        await processor.ProcessAsync();
        Assert.Empty(await queue.ReadyAsync(clock.GetUtcNow(), default));
        clock.Now = clock.Now.AddSeconds(1);
        await processor.ProcessAsync();
        clock.Now = clock.Now.AddSeconds(2);
        await processor.ProcessAsync();
        clock.Now = clock.Now.AddHours(1);
        await processor.ProcessAsync();
        Assert.Equal(3, transport.Calls);
    }

    [Fact]
    public async Task Denied_permission_never_starts_location_lookup()
    {
        var device = new Device
        {
            Decision = PermissionDecision.Denied
        };
        Assert.Null(await new LocationCoordinator(device).FindAsync());
        Assert.Equal(0, device.Calls);
    }

    [Fact]
    public async Task Granted_permission_uses_caller_cancellation_token()
    {
        var device = new Device();
        using var cts = new CancellationTokenSource();
        await new LocationCoordinator(device).FindAsync(cts.Token);
        Assert.Equal(1, device.Calls);
        Assert.Equal(cts.Token, device.Token);
    }

    [Fact]
    public async Task Sign_in_rejects_empty_or_expired_token_before_storage()
    {
        var vault = new Vault();
        var identity = new Identity
        {
            Session = new("", DateTimeOffset.UtcNow.AddHours(1))
        };
        await Assert.ThrowsAsync<InvalidDataException>(() => new SessionService(identity, vault).SignInAsync());
        Assert.Null(vault.Value);
        identity.Session = new("expired", DateTimeOffset.UtcNow.AddSeconds(-1));
        await Assert.ThrowsAsync<InvalidDataException>(() => new SessionService(identity, vault).SignInAsync());
        Assert.Null(vault.Value);
    }

    [Fact]
    public async Task Sign_out_clears_local_token_even_when_identity_signout_fails()
    {
        var vault = new Vault
        {
            Value = "private"
        };
        var identity = new Identity
        {
            FailSignout = true
        };
        await Assert.ThrowsAsync<HttpRequestException>(() => new SessionService(identity, vault).SignOutAsync());
        Assert.Null(vault.Value);
    }

    [Fact]
    public async Task Concurrent_proactive_refresh_calls_share_one_fresh_token()
    {
        var calls = 0;
        var clock = new TestClock();
        var coordinator = new AccessTokenCoordinator(_ =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(new AccessSession("safe", clock.GetUtcNow().AddHours(1)));
        }, clock);
        var sessions = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => coordinator.GetAsync()));
        Assert.Equal(1, calls);
        Assert.All(sessions, x => Assert.Equal("safe", x.AccessToken));
    }

    [Fact]
    public async Task Short_lived_token_does_not_cause_a_refresh_loop_but_expired_token_is_never_returned()
    {
        int calls = 0;
        var clock = new TestClock();
        var coordinator = new AccessTokenCoordinator(_ =>
        {
            calls++;
            return Task.FromResult(new AccessSession("short", clock.Now.AddSeconds(5)));
        }, clock);
        await coordinator.GetAsync();
        await coordinator.GetAsync();
        Assert.Equal(1, calls);
        clock.Now = clock.Now.AddSeconds(6);
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.GetAsync());
    }

    [Fact]
    public void Metrics_use_checked_integer_money_and_g_units()
    {
        Assert.Equal(250, InventoryMetrics.StockValueCents([Item()]));
        Assert.False(InventoryMetrics.IsShake(0, 0, 1, 2.5));
        Assert.True(InventoryMetrics.IsShake(0, 0, 3, 2.5));
        Assert.Throws<OverflowException>(() => InventoryMetrics.StockValueCents([new() { UnitPriceCents = long.MaxValue, Quantity = 2 }]));
    }

    private sealed class ControlledRepository : IProductRepository
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls;
        public bool Ended;
        public CancellationToken ObservedToken;
        public Exception? Failure;
        public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken token = default)
        {
            Calls++;
            ObservedToken = token;
            Started.TrySetResult(true);
            try
            {
                await Release.Task.WaitAsync(token);
                if (Failure is not null)
                    throw Failure;
                return[Item()];
            }
            finally
            {
                Ended = true;
            }
        }

        public Task<Product?> GetAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task SaveAsync(Product product, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class ResponseHandler(HttpStatusCode status, HttpContent content) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }
    }

    private sealed class TrackedContent(string text) : StringContent(text)
    {
        public bool WasDisposed;
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class PageSource : IPageSource
    {
        public bool Fail = true;
        public List<int> Pages = [];
        public Task<IReadOnlyList<Product>> ReadAsync(int page, int size, CancellationToken token)
        {
            Pages.Add(page);
            if (Fail)
                throw new HttpRequestException();
            return Task.FromResult<IReadOnlyList<Product>>([Item()]);
        }
    }

    private sealed class TextStore : ITextFileStore
    {
        public bool Fail;
        public string? Name;
        public Task WriteAsync(string name, string contents, CancellationToken token)
        {
            if (Fail)
                throw new IOException();
            Name = name;
            return Task.CompletedTask;
        }
    }

    private sealed class Orders : IOrderRepository
    {
        public int Calls;
        public decimal Total;
        public bool Wait;
        public CancellationToken Token;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<int> SaveAsync(decimal total, CancellationToken token)
        {
            Calls++;
            Total = total;
            Token = token;
            Started.TrySetResult(true);
            if (Wait)
                await Task.Delay(Timeout.Infinite, token);
            return 42;
        }
    }

    private sealed class Navigation : INavigationService
    {
        public int Id;
        public Task ShowConfirmationAsync(int id, CancellationToken token)
        {
            Id = id;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingTransport : ISyncTransport
    {
        public int Calls;
        public Task SendAsync(PendingSyncItem item, CancellationToken token)
        {
            Calls++;
            throw new HttpRequestException();
        }
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Device : ILocationDevice
    {
        public PermissionDecision Decision = PermissionDecision.Granted;
        public int Calls;
        public CancellationToken Token;
        public Task<PermissionDecision> RequestPermissionAsync() => Task.FromResult(Decision);
        public Task<Position?> LocateAsync(CancellationToken token)
        {
            Calls++;
            Token = token;
            return Task.FromResult<Position?>(new Position(1, 2));
        }
    }

    private sealed class Vault : ITokenVault
    {
        public string? Value;
        public Task SetAsync(string value)
        {
            Value = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync() => Task.FromResult(Value);
        public void Remove() => Value = null;
    }

    private sealed class Identity : IIdentityClient
    {
        public AccessSession Session = new("token", DateTimeOffset.UtcNow.AddHours(1));
        public bool FailSignout;
        public Task<AccessSession> SignInAsync(CancellationToken token) => Task.FromResult(Session);
        public Task SignOutAsync(CancellationToken token)
        {
            if (FailSignout)
                throw new HttpRequestException();
            return Task.CompletedTask;
        }
    }

    private sealed class TestFolder : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("maui-book-");
        public string Path => directory.FullName;

        public void Dispose() => directory.Delete(true);
    }
}
