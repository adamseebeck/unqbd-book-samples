namespace MauiInventory.App;
public sealed class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        Title = "Settings";
        var toggle = new Switch
        {
            IsToggled = Preferences.Default.Get("compact_view", false)
        };
        toggle.Toggled += (_, e) => Preferences.Default.Set("compact_view", e.Value);
        SemanticProperties.SetDescription(toggle, "Use a compact inventory view");
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Compact view",
                    FontSize = 20
                },
                toggle,
                new Label
                {
                    Text = "This preference contains no credentials. Credentials belong in secure storage."
                }
            }
        };
    }
}
