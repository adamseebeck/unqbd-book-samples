namespace MauiInventory.App;
public partial class AdaptiveSettingsPage : ContentPage
{
    private string state = "";
    public AdaptiveSettingsPage() => InitializeComponent();
    private async void OpenResources(object? sender, EventArgs e)
    {
        if (sender is not Button button || !button.IsEnabled)
            return;
        button.IsEnabled = false;
        try
        {
            await Navigation.PushAsync(new ResourcePracticePage());
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Navigation unavailable", "Try again.", "OK");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (AdaptiveGrid is null)
            return;
        var next = width < 600 ? "Narrow" : "Wide";
        if (next == state)
            return;
        AdaptiveGrid.ColumnDefinitions = next == "Narrow" ? [new(GridLength.Star)] : [new(new GridLength(250)), new(GridLength.Star)];
        VisualStateManager.GoToState(AdaptiveGrid, next);
        state = next;
    }
}
