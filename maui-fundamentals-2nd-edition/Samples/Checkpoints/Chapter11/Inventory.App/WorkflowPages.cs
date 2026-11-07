using System.Globalization;
using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class WorkflowNavigationService(WorkflowState state) : IWorkflowNavigationService
{
    private readonly WorkflowResetCoordinator reset = new(state, async () =>
    {
        await Shell.Current.GoToAsync("../..");
        if (Shell.Current.CurrentPage is not CartPage)
            throw new InvalidOperationException("The reset did not reach its cart.");
    }, () => Preferences.Default.Set("workflow_screen", "cart"));
    public Task CheckoutAsync(decimal total) => Shell.Current.GoToAsync("checkout?total=" + Uri.EscapeDataString(total.ToString(CultureInfo.InvariantCulture)));
    public async Task ConfirmAsync()
    {
        await Shell.Current.GoToAsync("confirmation");
        if (Shell.Current.CurrentPage is not ConfirmationPage)
            throw new InvalidOperationException("Confirmation navigation did not complete.");
        state.Completed = true;
        Preferences.Default.Set("workflow_screen", "confirmation");
    }

    public Task BackAsync() => Shell.Current.GoToAsync("..");
    public Task ResetAsync() => reset.ResetAsync();
}

public sealed class DialogService : IDialogService
{
    public Task ShowAsync(string title, string message) => Shell.Current.DisplayAlertAsync(title, message, "OK");
}

public sealed class SecureStatePersistenceService
{
    public string ReadScreen() => Preferences.Default.Get("workflow_screen", "cart");
    public void SaveScreen(string screen)
    {
        if (screen is not ("cart" or "checkout" or "confirmation"))
            throw new ArgumentException("Unknown workflow screen.");
        Preferences.Default.Set("workflow_screen", screen);
    }

    public Task SaveSensitiveDemoAsync() => SecureStorage.Default.SetAsync("workflow_demo_sensitive", "local-learning-value");
}

public sealed class CartPage : ContentPage
{
    private bool navigating;
    private readonly List<Button> navigationButtons = [];
    public CartPage(IWorkflowNavigationService navigation, SecureStatePersistenceService storage)
    {
        Title = "Fictional cart";
        var previous = storage.ReadScreen();
        storage.SaveScreen("cart");
        var total = new Entry
        {
            Text = "12.50",
            Placeholder = "Fictional total, use a period: 12.50",
            Keyboard = Keyboard.Numeric
        };
        var next = new Button
        {
            Text = "Continue to checkout"
        };
        next.Clicked += async (_, _) => await NavigateAsync(async () =>
        {
            if (WorkflowTotal.TryRead(total.Text, out var amount))
                await navigation.CheckoutAsync(amount);
            else
                await Shell.Current.GoToAsync("checkout?total=invalid");
        });
        var invalid = new Button
        {
            Text = "Try a missing total"
        };
        invalid.Clicked += async (_, _) => await NavigateAsync(() => Shell.Current.GoToAsync("checkout"));
        navigationButtons.Add(next);
        navigationButtons.Add(invalid);
        var sensitive = new Button
        {
            Text = "Store a local sensitive demonstration value"
        };
        sensitive.Clicked += async (_, _) =>
        {
            try
            {
                await storage.SaveSensitiveDemoAsync();
                await DisplayAlertAsync("Stored", "The fixed demonstration value was stored securely, not in navigation preferences.", "OK");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Storage unavailable", "Secure storage could not complete.", "OK");
            }
        };
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = $"Prior saved screen: {previous}. This new fictional order begins safely at Cart."},
                total,
                next,
                invalid,
                sensitive
            }
        };
    }

    private async Task NavigateAsync(Func<Task> action)
    {
        if (navigating)
            return;
        navigating = true;
        foreach (var button in navigationButtons)
            button.IsEnabled = false;
        try
        {
            await action();
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Navigation unavailable", "Try again.", "OK");
        }
        finally
        {
            navigating = false;
            foreach (var button in navigationButtons)
                button.IsEnabled = true;
        }
    }
}

public sealed class CheckoutPage : ContentPage, IQueryAttributable
{
    private readonly CheckoutViewModel vm;
    private readonly SecureStatePersistenceService storage;
    private readonly Label amount = new();
    public CheckoutPage(CheckoutViewModel vm, IWorkflowNavigationService navigation, SecureStatePersistenceService storage)
    {
        this.vm = vm;
        this.storage = storage;
        BindingContext = vm;
        Title = "Checkout simulation";
        var confirm = new Button
        {
            Text = "Confirm fictional order"
        };
        confirm.SetBinding(Button.CommandProperty, nameof(vm.ConfirmCommand));
        var error = new Label();
        error.SetBinding(Label.TextProperty, nameof(vm.ErrorMessage));
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                amount,
                confirm,
                error,
                new Label
                {
                    Text = "No order, charge or message is sent."
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) => vm.Receive(query);
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (await vm.InitializeAsync())
            {
                storage.SaveScreen("checkout");
                amount.Text = $"Fictional total: {vm.Total:C}";
            }
        }
        catch (Exception)
        {
            amount.Text = "Could not initialize checkout.";
        }
    }
}

public sealed class ConfirmationPage : ContentPage
{
    public ConfirmationPage(IWorkflowNavigationService navigation, SecureStatePersistenceService storage)
    {
        Title = "Fictional confirmation";
        var reset = new Button
        {
            Text = "Start a new fictional cart"
        };
        reset.Clicked += async (_, _) =>
        {
            reset.IsEnabled = false;
            try
            {
                await navigation.ResetAsync();
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Navigation unavailable", "Try again.", "OK");
            }
            finally
            {
                reset.IsEnabled = true;
            }
        };
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "The local workflow is complete. Its back operation is guarded; use reset to start again."
                },
                reset
            }
        };
    }
}
