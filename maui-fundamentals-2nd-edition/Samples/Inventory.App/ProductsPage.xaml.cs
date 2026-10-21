using MauiInventory.Core;

namespace MauiInventory.App;
public partial class ProductsPage : ContentPage
{
    private readonly ProductListViewModel viewModel;
    private bool navigating;
    private bool active;
    private int visit;
    public ProductsPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        active = true;
        var current = ++visit;
        // Wait for the preceding canceled appearance before starting a fresh command.
        var previous = viewModel.LoadCommand.ExecutionTask;
        if (previous is not null)
            await previous;
        if (active && current == visit)
            await viewModel.LoadCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        active = false;
        visit++;
        viewModel.LoadCommand.Cancel();
        base.OnDisappearing();
    }

    private async void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (navigating || e.CurrentSelection.FirstOrDefault()is not Product product)
            return;
        navigating = true;
        try
        {
            await Shell.Current.GoToAsync(ProductRoute.Detail(product.Id));
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Navigation unavailable", "Try selecting the product again.", "OK");
        }
        finally
        {
            ((CollectionView)sender!).SelectedItem = null;
            navigating = false;
        }
    }
}
