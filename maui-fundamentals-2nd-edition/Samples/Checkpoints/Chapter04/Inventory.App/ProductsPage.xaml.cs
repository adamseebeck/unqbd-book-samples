using MauiInventory.Core;

namespace MauiInventory.App;
public partial class ProductsPage : ContentPage
{
    private readonly IProductRepository repository;
    private CancellationTokenSource? loading;
    private bool navigating;
    public ProductsPage(IProductRepository repository)
    {
        InitializeComponent();
        this.repository = repository;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadProducts();
    }

    private void OnSearch(object? sender, EventArgs e) => LoadProducts();

    private async void LoadProducts()
    {
        loading?.Cancel();
        var current = loading = new CancellationTokenSource();
        var query = SearchBox.Text ?? "";
        try
        {
            var products = await repository.ListAsync(current.Token);
            if (!current.IsCancellationRequested)
                ProductList.ItemsSource = products
                    .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!current.IsCancellationRequested)
                await DisplayAlertAsync("Products unavailable", "Could not load products. Try again.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        loading?.Cancel();
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
