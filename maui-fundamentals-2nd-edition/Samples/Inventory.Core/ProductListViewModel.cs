using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MauiInventory.Core;
public partial class ProductListViewModel : ObservableObject
{
    private readonly IProductRepository repository;
    private int running;
    public ObservableCollection<Product> Products { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private bool isBusy;
    public bool IsIdle => !IsBusy;

    [ObservableProperty]
    private string errorMessage = "";
    [ObservableProperty]
    private string query = "";
    public ProductListViewModel(IProductRepository repository) => this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private bool CanLoad() => !IsBusy;
    [RelayCommand(CanExecute = nameof(CanLoad), IncludeCancelCommand = true)]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        // Protect direct domain calls too. Programmatic command ExecuteAsync can cancel
        // the previous command token; UI command callers should honor CanExecute.
        if (Interlocked.Exchange(ref running, 1) != 0)
            return;
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            var rows = await repository.ListAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var selected = rows.Where(p => p.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(Query, StringComparison.OrdinalIgnoreCase)).ToArray();
            Products.Clear();
            foreach (var product in selected)
                Products.Add(product);
        }
        catch (OperationCanceledException)when (cancellationToken.IsCancellationRequested)
        {
            ErrorMessage = "Loading canceled.";
        }
        catch (Exception)
        {
            ErrorMessage = "Could not load products. Try again.";
        }
        finally
        {
            IsBusy = false;
            Volatile.Write(ref running, 0);
        }
    }
}

public static class ProductRoute
{
    public static string Detail(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));
        return $"details?id={id}";
    }

    public static bool TryRead(object? value, out int id) => int.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out id) && id > 0;
}
