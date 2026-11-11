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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        loading?.Cancel();
        var current = loading = new CancellationTokenSource();
        try
        {
            var products = await repository.ListAsync(current.Token);
            if (!current.IsCancellationRequested)
                ProductList.ItemsSource = products;
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!current.IsCancellationRequested)
                await DisplayAlertAsync("Products unavailable", "Could not load products. Try again.", "OK");
        }
        finally
        {
            if (ReferenceEquals(loading, current)) loading = null;
            current.Dispose();
        }
    }

    protected override void OnDisappearing()
    {
        loading?.Cancel();
        base.OnDisappearing();
    }

    private async void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (navigating || e.CurrentSelection.FirstOrDefault() is not Product product)
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
