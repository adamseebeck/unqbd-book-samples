using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace MauiInventory.Core;
public sealed class WorkflowState
{
    public bool Completed { get; set; }
    public bool Resetting { get; set; }
}

public interface IWorkflowNavigationService
{
    Task CheckoutAsync(decimal total);
    Task ConfirmAsync();
    Task BackAsync();
    Task ResetAsync();
}

public interface IDialogService
{
    Task ShowAsync(string title, string message);
}

public static class WorkflowTotal
{
    public static bool TryRead(object? value, out decimal total) => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out total) && total >= 0;
}

public sealed class WorkflowResetCoordinator(WorkflowState state, Func<Task> navigate, Action persist)
{
    private int running;
    public async Task ResetAsync()
    {
        if (Interlocked.Exchange(ref running, 1) != 0)
            return;
        state.Resetting = true;
        try
        {
            await navigate();
            state.Completed = false;
            persist();
        }
        finally
        {
            state.Resetting = false;
            Volatile.Write(ref running, 0);
        }
    }
}

public partial class CheckoutViewModel(IWorkflowNavigationService navigation, IDialogService dialogs) : ObservableObject
{
    private bool valid;
    private int confirming;
    public decimal Total { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool isReady;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool isConfirming;
    [ObservableProperty]
    private string errorMessage = "";
    public void Receive(IDictionary<string, object> query)
    {
        IsReady = false;
        ErrorMessage = "";
        decimal total = 0;
        valid = query.TryGetValue("total", out var value) && WorkflowTotal.TryRead(value, out total);
        Total = valid ? total : 0;
    }

    public async Task<bool> InitializeAsync()
    {
        if (valid)
        {
            IsReady = true;
            return true;
        }

        await dialogs.ShowAsync("Invalid total", "A nonnegative total is required.");
        await navigation.BackAsync();
        return false;
    }

    private bool CanConfirm() => IsReady && !IsConfirming;
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    public async Task ConfirmAsync()
    {
        if (!IsReady || Interlocked.Exchange(ref confirming, 1) != 0)
            return;
        try
        {
            IsConfirming = true;
            ErrorMessage = "";
            await navigation.ConfirmAsync();
            IsReady = false;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not confirm this fictional workflow. Try again.";
        }
        finally
        {
            IsConfirming = false;
            Volatile.Write(ref confirming, 0);
        }
    }
}
