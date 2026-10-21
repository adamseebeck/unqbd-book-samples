using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class DetailsPage : ContentPage, IQueryAttributable
{
    private readonly IProductRepository repository;
    private readonly Label name = new()
    {
        FontSize = 24
    };
    private int productId;
    private CancellationTokenSource? appearance;
    public DetailsPage(IProductRepository repository)
    {
        this.repository = repository;
        Title = "Product details";
        SemanticProperties.SetHeadingLevel(name, SemanticHeadingLevel.Level1);
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Children =
            {
                name
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        productId = query.TryGetValue("id", out var value) && ProductRoute.TryRead(value, out var id) ? id : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Cancel();
        appearance?.Dispose();
        var current = appearance = new CancellationTokenSource();
        name.Text = "Loading product…";
        try
        {
            var product = productId > 0 ? await repository.GetAsync(productId, current.Token) : null;
            if (!current.IsCancellationRequested)
                name.Text = product?.Name ?? "Product not found.";
        }
        catch (OperationCanceledException)when (current.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!current.IsCancellationRequested)
                name.Text = "Could not load product.";
        }
    }

    protected override void OnDisappearing()
    {
        appearance?.Cancel();
        base.OnDisappearing();
    }
}
