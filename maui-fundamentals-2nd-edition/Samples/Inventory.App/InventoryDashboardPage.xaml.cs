using MauiInventory.Core;

namespace MauiInventory.App;
public partial class InventoryDashboardPage : ContentPage
{
    private readonly DemoPageSource source = new();
    private readonly PagedProductsViewModel viewModel;
    private bool active;
    private int visit;
    public InventoryDashboardPage()
    {
        InitializeComponent();
        viewModel = new(source);
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        active = true;
        var current = ++visit;
        try
        {
            var previous = viewModel.LoadMoreCommand.ExecutionTask;
            if (previous is not null)
                try
                {
                    await previous;
                }
                catch (OperationCanceledException)
                {
                }

            if (active && current == visit)
                await LoadObservedAsync();
        }
        catch (Exception)
        {
            if (active && current == visit)
                await DisplayAlertAsync("Loading unavailable", "Try loading this page again.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        active = false;
        visit++;
        viewModel.LoadMoreCommand.Cancel();
        base.OnDisappearing();
    }

    private async void FailNext(object? sender, EventArgs e)
    {
        if (!active || !viewModel.LoadMoreCommand.CanExecute(null))
            return;
        source.FailNext = true;
        await LoadObservedAsync();
    }

    private async void LoadNext(object? sender, EventArgs e) => await LoadObservedAsync();
    private async Task LoadObservedAsync()
    {
        if (!active || !viewModel.LoadMoreCommand.CanExecute(null))
            return;
        try
        {
            await viewModel.LoadMoreCommand.ExecuteAsync(null);
        }
        catch (OperationCanceledException)when (!active || viewModel.LoadMoreCommand.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Loading unavailable", "Try loading this page again.", "OK");
        }
    }
}
