using MauiInventory.Core;
using Xunit;

namespace Inventory.Tests;
public sealed class WorkflowTests
{
    [Theory]
    [InlineData("12,50")]
    [InlineData("1,2,3")]
    [InlineData("1e4")]
    [InlineData("NaN")]
    public void Workflow_total_rejects_ambiguous_grouping_or_nonnumeric_input(string text) => Assert.False(WorkflowTotal.TryRead(text, out _));
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("-1")]
    public async Task Invalid_total_cannot_confirm_even_if_return_navigation_fails(string? total)
    {
        var navigation = new Navigation
        {
            BackFails = true
        };
        var dialogs = new Dialogs();
        var vm = new CheckoutViewModel(navigation, dialogs);
        vm.Receive(total is null ? [] : new Dictionary<string, object> { ["total"] = total });
        await Assert.ThrowsAsync<IOException>(() => vm.InitializeAsync());
        Assert.Equal(1, dialogs.Calls);
        Assert.False(vm.ConfirmCommand.CanExecute(null));
        await vm.ConfirmAsync();
        Assert.Equal(0, navigation.Confirms);
    }

    [Fact]
    public async Task Valid_confirmation_is_guarded_during_real_overlap_and_disables_after_success()
    {
        var navigation = new Navigation();
        var vm = new CheckoutViewModel(navigation, new Dialogs());
        vm.Receive(new Dictionary<string, object> { ["total"] = "12.50" });
        Assert.True(await vm.InitializeAsync());
        Assert.Equal(12.5m, vm.Total);
        var first = vm.ConfirmAsync();
        await navigation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.ConfirmAsync();
        Assert.Equal(1, navigation.Confirms);
        Assert.False(vm.ConfirmCommand.CanExecute(null));
        navigation.Release.TrySetResult(true);
        await first;
        Assert.False(vm.IsReady);
        Assert.False(vm.IsConfirming);
    }

    [Fact]
    public async Task Failed_reset_preserves_completed_guard_and_success_persists_after_navigation()
    {
        var state = new WorkflowState
        {
            Completed = true
        };
        int writes = 0;
        var failed = new WorkflowResetCoordinator(state, () => throw new IOException(), () => writes++);
        await Assert.ThrowsAsync<IOException>(() => failed.ResetAsync());
        Assert.True(state.Completed);
        Assert.False(state.Resetting);
        Assert.Equal(0, writes);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var successful = new WorkflowResetCoordinator(state, async () =>
        {
            started.TrySetResult(true);
            await release.Task;
        }, () => writes++);
        var reset = successful.ResetAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(state.Resetting);
        Assert.True(state.Completed);
        Assert.Equal(0, writes);
        release.TrySetResult(true);
        await reset;
        Assert.False(state.Completed);
        Assert.False(state.Resetting);
        Assert.Equal(1, writes);
    }

    [Fact]
    public async Task Paging_command_cancellation_retains_rows_and_a_new_visit_can_load()
    {
        var source = new PageSource();
        var vm = new PagedProductsViewModel(source);
        var operation = vm.LoadMoreCommand.ExecuteAsync(null);
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.LoadMoreCommand.Cancel();
        await operation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(vm.Products);
        Assert.False(vm.IsLoading);
        source.Pause = false;
        await vm.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(1, Assert.Single(vm.Products).Id);
        Assert.Equal(new[] { 0, 0 }, source.Pages);
    }

    [Fact]
    public async Task Category_canceled_load_is_drained_before_reentry_load_publishes_rows()
    {
        var repository = new PausedCategories();
        var vm = new CategoryViewModel(repository, new UnusedFiles(), new UnusedSettings());
        var previous = vm.LoadCommand.ExecuteAsync(null);
        await repository.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.LoadCommand.Cancel();
        Assert.False(previous.IsCompleted);
        Assert.False(vm.LoadCommand.CanExecute(null));
        repository.Release.TrySetResult(true);
        await previous.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(vm.Categories);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal(2, repository.Calls);
        Assert.Equal("Current", Assert.Single(vm.Categories).Name);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Real_demo_source_supplies_stock_boundaries_and_a_final_short_page()
    {
        var source = new DemoPageSource();
        var first = await source.ReadAsync(0, 20, default);
        var second = await source.ReadAsync(1, 20, default);
        var last = await source.ReadAsync(2, 20, default);
        Assert.Equal(new[] { 10, 50, 51, 9 }, first.Take(4).Select(row => row.Quantity));
        Assert.Equal(20, first.Count);
        Assert.Equal(20, second.Count);
        Assert.Equal(5, last.Count);
        Assert.Equal(45, first.Concat(second).Concat(last).Select(row => row.Id).Distinct().Count());
        Assert.Empty(await source.ReadAsync(3, 20, default));
    }

    [Fact]
    public async Task Real_demo_source_consumes_one_failure_and_retry_requests_the_same_page()
    {
        var source = new DemoPageSource
        {
            FailNext = true
        };
        await Assert.ThrowsAsync<HttpRequestException>(() => source.ReadAsync(0, 20, default));
        Assert.False(source.FailNext);
        var retry = await source.ReadAsync(0, 20, default);
        Assert.Equal(Enumerable.Range(1, 20), retry.Select(row => row.Id));
    }

    private sealed class PausedCategories : ICategoryRepository
    {
        public int Calls;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken token = default)
        {
            if (++Calls == 1)
            {
                Started.TrySetResult(true);
                await Release.Task; // Model a native call that cannot be interrupted mid-operation.
            }

            return[new Category
            {
                Name = "Current"
            }

            ];
        }

        public Task AddAsync(Category category, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class UnusedFiles : ITextFileStore
    {
        public Task WriteAsync(string name, string contents, CancellationToken token) => throw new NotSupportedException();
    }

    private sealed class UnusedSettings : IExportSettings
    {
        public void SetLastExport(DateTimeOffset timestamp) => throw new NotSupportedException();
    }

    private sealed class Navigation : IWorkflowNavigationService
    {
        public bool BackFails;
        public int Confirms;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task CheckoutAsync(decimal total) => Task.CompletedTask;
        public async Task ConfirmAsync()
        {
            Confirms++;
            Started.TrySetResult(true);
            await Release.Task;
        }

        public Task BackAsync() => BackFails ? throw new IOException() : Task.CompletedTask;
        public Task ResetAsync() => Task.CompletedTask;
    }

    private sealed class Dialogs : IDialogService
    {
        public int Calls;
        public Task ShowAsync(string title, string message)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class PageSource : IPageSource
    {
        public bool Pause = true;
        public List<int> Pages = [];
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<IReadOnlyList<Product>> ReadAsync(int page, int size, CancellationToken token)
        {
            Pages.Add(page);
            Started.TrySetResult(true);
            if (Pause)
                await Task.Delay(Timeout.Infinite, token);
            return[new Product
            {
                Id = 1,
                Sku = "A",
                Name = "A"
            }

            ];
        }
    }
}
