using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace MauiInventory.Core;
public interface IOrderRepository
{
    Task<int> SaveAsync(decimal total, CancellationToken token);
}

public interface INavigationService
{
    Task ShowConfirmationAsync(int orderId, CancellationToken token);
}

public partial class OrderViewModel(IOrderRepository orders, INavigationService navigation) : ObservableObject
{
    private int submitting;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitOrderCommand))]
    private bool isValid;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitOrderCommand))]
    private decimal total;
    private bool CanSubmit() => IsValid && Total >= 0;
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    public async Task SubmitOrderAsync(CancellationToken token)
    {
        if (!CanSubmit() || Interlocked.Exchange(ref submitting, 1) != 0)
            return;
        try
        {
            var id = await orders.SaveAsync(Total, token);
            token.ThrowIfCancellationRequested();
            await navigation.ShowConfirmationAsync(id, token);
        }
        finally
        {
            Volatile.Write(ref submitting, 0);
        }
    }
}
