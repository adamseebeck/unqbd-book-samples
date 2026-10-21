using MauiInventory.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MauiInventory.App;
public sealed class PracticePage : ContentPage
{
    private readonly Label result = new()
    {
        Text = "Choose an exercise."
    };
    private CancellationTokenSource? appearance;
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
                        Text = $"Platform: {DeviceInfo.Platform}. SDK/runtime: {Environment.Version}"},
                    new BorderlessEntry
                    {
                        Placeholder = "Borderless entry exercise"
                    },
                    new QuantityPicker(),
                    ActivityButton(),
                    Button("Save and reload SQLite", DatabaseAsync),
                    Button("Read the demonstration API", NetworkAsync),
                    Button("Find my current location", LocationAsync),
                    Button("Choose a photo", PhotoAsync),
                    Button("Sign in to your configured tenant", SignInAsync),
                    Button("Sign out", SignOutAsync),
                    new GradientButton
                    {
                        Text = "Gradient handler exercise"
                    },
                    result
                }
            }
        };
    }

    private Button ActivityButton()
    {
        var button = new Button
        {
            Text = "Open chapter activities"
        };
        button.Clicked += async (_, _) =>
        {
            try
            {
                await Navigation.PushAsync(new ActivityGallery(services));
            }
            catch (Exception)
            {
                result.Text = "Could not open activities.";
            }
        };
        return button;
    }

    private Button Button(string text, Func<CancellationToken, Task<string>> action)
    {
        var button = new Button
        {
            Text = text
        };
        button.Clicked += async (_, _) =>
        {
            if (appearance is null)
                return;
            var token = appearance.Token;
            button.IsEnabled = false;
            try
            {
                var message = await action(token);
                if (!token.IsCancellationRequested)
                    result.Text = message;
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
            }
            catch (PermissionException)
            {
                if (!token.IsCancellationRequested)
                    result.Text = "Permission was denied. You can try again from Settings.";
            }
            catch (FeatureNotSupportedException)
            {
                if (!token.IsCancellationRequested)
                    result.Text = "This feature is unavailable on this device.";
            }
            catch (FeatureNotEnabledException)
            {
                if (!token.IsCancellationRequested)
                    result.Text = "Turn on the device feature and try again.";
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested)
                    result.Text = "The operation could not complete. Check the chapter setup and try again.";
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        return button;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Dispose();
        appearance = new CancellationTokenSource();
    }

    protected override void OnDisappearing()
    {
        appearance?.Cancel();
        base.OnDisappearing();
    }

    private async Task<string> DatabaseAsync(CancellationToken token)
    {
        var repo = services.GetRequiredService<SqliteProductRepository>();
        var existing = (await repo.ListAsync(token)).FirstOrDefault(p => p.Sku == "PRACTICE");
        await repo.SaveAsync(existing ?? new Product { Sku = "PRACTICE", Name = "Practice product", Quantity = 1, UnitPriceCents = 125 }, token);
        var rows = await repo.ListAsync(token);
        return $"SQLite contains {rows.Count} product(s).";
    }

    private async Task<string> NetworkAsync(CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        var rows = await services.GetRequiredService<CatalogClient>().GetAsync(token);
        return $"Demonstration response: {rows.Count} product(s), {watch.ElapsedMilliseconds} ms. This is not a device benchmark.";
    }

    private async Task<string> LocationAsync(CancellationToken token)
    {
        var position = await services.GetRequiredService<LocationCoordinator>().FindAsync(token);
        return position is null ? "No location was returned. Permission may be denied or a fix unavailable." : "A location was obtained. It is not logged or uploaded.";
    }

    private static async Task<string> PhotoAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var photo = (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 })).FirstOrDefault();
        token.ThrowIfCancellationRequested();
        return photo is null ? "Photo selection canceled." : "Photo selected. No copy or upload was made.";
    }

    private async Task<string> SignInAsync(CancellationToken token)
    {
        await services.GetRequiredService<SessionService>().SignInAsync(token);
        return "Signed in. The token is not displayed.";
    }

    private async Task<string> SignOutAsync(CancellationToken token)
    {
        await services.GetRequiredService<SessionService>().SignOutAsync(token);
        return "The app token and MSAL account cache entries were removed.";
    }
}
