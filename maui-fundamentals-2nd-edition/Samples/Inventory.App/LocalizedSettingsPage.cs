using System.Globalization;
using System.Resources;
using System.Text.Json;

namespace MauiInventory.App;
public static class AppResources
{
    private static readonly ResourceManager Manager = new("MauiInventory.App.Resources.Strings.AppResources", typeof(AppResources).Assembly);
    public static string Get(string key, CultureInfo culture) => Manager.GetString(key, culture) ?? throw new InvalidDataException($"Missing resource key {key}.");
}

public sealed class LocalizedSettingsPage : ContentPage
{
    private readonly Label configuration = new();
    private readonly CultureInfo culture;
    public LocalizedSettingsPage(bool spanish = false)
    {
        culture = CultureInfo.GetCultureInfo(spanish ? "es-ES" : "en-US");
        Title = AppResources.Get("SettingsTitle", culture);
        var heading = new Label
        {
            Text = AppResources.Get("SectionHeading", culture),
            FontSize = 24
        };
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level2);
        var decoration = new BoxView
        {
            Color = Colors.Gray,
            HeightRequest = 1
        };
        AutomationProperties.SetIsInAccessibleTree(decoration, false);
        var compact = new Switch
        {
            IsToggled = Preferences.Default.Get("CompactView", false)
        };
        SemanticProperties.SetDescription(compact, AppResources.Get("CompactLabel", culture));
        compact.Toggled += (_, e) => Preferences.Default.Set("CompactView", e.Value);
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    heading,
                    decoration,
                    new Label
                    {
                        Text = AppResources.Get("CompactLabel", culture)
                    },
                    compact,
                    new Label
                    {
                        Text = AppResources.Get("DemoValue", culture) + ": ••••"
                    },
                    configuration,
                    new Label
                    {
                        Text = AppResources.Get("ContrastInstruction", culture)
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("public-settings.json");
            var settings = await JsonSerializer.DeserializeAsync<PublicSettings>(stream);
            configuration.Text = settings is null ? AppResources.Get("ConfigurationUnavailable", culture) : string.Format(culture, AppResources.Get("PublicConfiguration", culture), settings.FeatureName);
        }
        catch (Exception)
        {
            configuration.Text = AppResources.Get("ConfigurationUnavailable", culture);
        }
    }

    private sealed class PublicSettings
    {
        public string FeatureName { get; set; } = "";
    }
}
