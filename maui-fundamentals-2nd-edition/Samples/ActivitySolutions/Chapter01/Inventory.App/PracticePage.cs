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
                    new GradientButton
                    {
                        Text = "Gradient handler exercise"
                    },
                    new GradientButton
                    {
                        Text = "Second gradient"
                    },
                    result
                }
            }
        };
    }
}
