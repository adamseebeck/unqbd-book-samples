namespace MauiInventory.App;
public sealed class PracticePage : ContentPage
{
    private readonly Label result = new()
    {
        Text = "Choose an exercise."
    };
    private readonly IServiceProvider services;
    public PracticePage(IServiceProvider services)
    {
        this.services = services;
        Title = "Practice";
        var heading = new Label
        {
            Text = "Inventory practice",
            FontSize = 24
        };
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    heading,
                    new Label
                    {
                        Text = $"{new PlatformGreeting().Describe()}. Runtime: {Environment.Version}"},
                    new BorderlessEntry
                    {
                        Placeholder = "Borderless entry exercise"
                    },
                    PageButton("Adaptive settings", () => new AdaptiveSettingsPage()),
                    PageButton("Stock dashboard", () => new InventoryDashboardPage()),
                    new GradientButton
                    {
                        Text = "Gradient handler exercise"
                    },
                    result
                }
            }
        };
    }

    private Button PageButton(string text, Func<Page> page)
    {
        var button = new Button
        {
            Text = text
        };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await Navigation.PushAsync(page());
            }
            catch (Exception)
            {
                result.Text = $"Could not open {text}.";
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        return button;
    }
}
