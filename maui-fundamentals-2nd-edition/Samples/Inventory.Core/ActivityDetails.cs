using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace MauiInventory.Core;
public partial class ProductDetailViewModel(IProductRepository repository) : ObservableObject
{
    private int id;
    private readonly SemaphoreSlim loadGate = new(1, 1);
    private long generation;
    [ObservableProperty]
    private Product? product;
    [ObservableProperty]
    private bool isLoading;
    [ObservableProperty]
    private string errorMessage = "";
    public async Task InitializeAsync(int productId, CancellationToken token = default)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));
        id = productId; // A refresh must use the latest accepted ID even while its load waits.
        var request = Interlocked.Increment(ref generation);
        await LoadCoreAsync(productId, request, token);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadProductAsync(CancellationToken token)
    {
        if (id <= 0)
            return;
        var request = Interlocked.Increment(ref generation);
        await LoadCoreAsync(id, request, token);
    }

    private async Task LoadCoreAsync(int requestedId, long request, CancellationToken token)
    {
        bool ownsGate = false;
        try
        {
            // A replacement waits for the old operation to unwind. Its ID is never lost
            // merely because the previous page appearance was still loading.
            await loadGate.WaitAsync(token);
            ownsGate = true;
            if (request != Volatile.Read(ref generation))
                return;
            id = requestedId;
            IsLoading = true;
            ErrorMessage = "";
            var result = await repository.GetAsync(requestedId, token);
            token.ThrowIfCancellationRequested();
            if (request != Volatile.Read(ref generation))
                return;
            Product = result;
            if (result is null)
                ErrorMessage = "Product not found.";
        }
        catch (OperationCanceledException)when (token.IsCancellationRequested)
        {
            if (request == Volatile.Read(ref generation))
                ErrorMessage = "Loading canceled.";
        }
        catch (Exception)
        {
            if (request == Volatile.Read(ref generation))
                ErrorMessage = "Could not load this product.";
        }
        finally
        {
            if (ownsGate)
            {
                IsLoading = false;
                loadGate.Release();
            }
        }
    }
}

public sealed class NavigationActivity
{
    public string Screen { get; private set; } = "cart";
    public decimal Total { get; private set; }

    public bool TryCheckout(string? total, out string error)
    {
        if (!decimal.TryParse(total, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            error = "Enter a nonnegative total.";
            return false;
        }

        Total = parsed;
        Screen = "checkout";
        error = "";
        return true;
    }

    public void Confirm()
    {
        if (Screen != "checkout")
            throw new InvalidOperationException("Checkout first.");
        Screen = "confirmation";
    }

    public bool GoBack()
    {
        if (Screen == "confirmation")
            return false;
        Screen = "cart";
        return true;
    }

    public void Reset() => Screen = "cart";
    public bool Restore(string? value)
    {
        if (value is not ("cart" or "checkout" or "confirmation"))
            return false;
        Screen = value;
        return true;
    }
}
