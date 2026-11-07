using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MauiInventory.Core;
public interface IPageSource
{
    Task<IReadOnlyList<Product>> ReadAsync(int page, int pageSize, CancellationToken token);
}

public partial class PagedProductsViewModel(IPageSource source) : ObservableObject
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private int nextPage;
    public ObservableCollection<Product> Products { get; } = [];

    [ObservableProperty]
    private bool isLoading;
    [ObservableProperty]
    private bool hasMore = true;
    [ObservableProperty]
    private string errorMessage = "";
    [RelayCommand(IncludeCancelCommand = true)]
    public async Task LoadMoreAsync(CancellationToken token = default)
    {
        if (!await gate.WaitAsync(0, token))
            return;
        try
        {
            if (!HasMore)
                return;
            IsLoading = true;
            ErrorMessage = "";
            var page = await source.ReadAsync(nextPage, 20, token);
            token.ThrowIfCancellationRequested();
            foreach (var item in page)
                if (!Products.Any(p => p.Id == item.Id))
                    Products.Add(item);
            nextPage++;
            HasMore = page.Count == 20;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Canceling keeps the rows that are already loaded.
        }
        catch (Exception)
        {
            ErrorMessage = "Could not load the next page. Try again.";
        }
        finally
        {
            IsLoading = false;
            gate.Release();
        }
    }
}
