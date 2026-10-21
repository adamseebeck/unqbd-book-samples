using MauiInventory.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiInventory.App;
public interface IThemeService
{
    string GetThemeColor();
}

public sealed class ThemeService : IThemeService
{
    public string GetThemeColor()
    {
#if ANDROID
        return "#512BD4";
#elif IOS || MACCATALYST
        return "#007AFF";
#else
        return "#0078D4";
#endif
    }
}

public sealed class ProfilePage : ContentPage
{
    public ProfilePage(IThemeService theme)
    {
        Title = "Profile";
        BackgroundColor = Color.FromArgb(theme.GetThemeColor());
        var platform = DeviceInfo.Platform.ToString();
        BindingContext = new
        {
            PlatformName = platform
        };
        var label = new Label
        {
            TextColor = Colors.White,
            FontSize = 24
        };
        label.SetBinding(Label.TextProperty, "PlatformName");
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Children =
            {
                label
            }
        };
    }
}

public sealed class ProfileEntryPage : ContentPage
{
    public ProfileEntryPage()
    {
        Title = "Profile route activity";
        var button = new Button
        {
            Text = "View Profile"
        };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await Shell.Current.GoToAsync("profile");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Profile unavailable", "Check the route and service registration.", "OK");
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Children =
            {
                button,
                new Label
                {
                    Text = "Open the profile, go back, then visit again. The route resolves the injected theme service on each new page."
                }
            }
        };
    }
}

public sealed class ProductDetailActivityPage : ContentPage, IQueryAttributable
{
    private readonly ProductDetailViewModel vm;
    private int id = 1;
    private CancellationTokenSource? appearance;
    public ProductDetailActivityPage(ProductDetailViewModel vm)
    {
        this.vm = vm;
        Title = "MVVM product detail";
        BindingContext = vm;
        var name = new Label
        {
            FontSize = 24
        };
        name.SetBinding(Label.TextProperty, "Product.Name");
        var quantity = new Label();
        quantity.SetBinding(Label.TextProperty, "Product.Quantity", stringFormat: "Stock: {0}");
        var price = new Label();
        price.SetBinding(Label.TextProperty, "Product.UnitPriceCents", converter: new CentsConverter());
        var busy = new ActivityIndicator();
        busy.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsLoading));
        var error = new Label();
        error.SetBinding(Label.TextProperty, nameof(vm.ErrorMessage));
        var refresh = new Button
        {
            Text = "Refresh"
        };
        refresh.SetBinding(Button.CommandProperty, nameof(vm.LoadProductCommand));
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12,
            Children =
            {
                name,
                quantity,
                price,
                busy,
                error,
                refresh
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) => id = query.TryGetValue("id", out var value) && ProductRoute.TryRead(value, out var parsed) ? parsed : 0;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Dispose();
        appearance = new();
        if (id <= 0)
        {
            await DisplayAlertAsync("Invalid product", "Choose a positive product identifier.", "OK");
            return;
        }

        await vm.InitializeAsync(id, appearance.Token);
    }

    protected override void OnDisappearing()
    {
        appearance?.Cancel();
        vm.LoadProductCommand.Cancel();
        base.OnDisappearing();
    }

    private sealed class CentsConverter : IValueConverter
    {
        public object Convert(object? value, Type type, object? parameter, CultureInfo culture) => value is long cents ? (cents / 100m).ToString("C", culture) : "";
        public object ConvertBack(object? value, Type type, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }
}

public sealed class AppTextFileStore : ITextFileStore
{
    public Task WriteAsync(string name, string contents, CancellationToken token)
    {
        if (name != "categories.csv")
            throw new ArgumentException("Unsupported export file.", nameof(name));
        return File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, name), contents, new System.Text.UTF8Encoding(false), token);
    }
}

public sealed class CategoriesPage : ContentPage
{
    private readonly CategoryViewModel vm;
    private bool active;
    private int visit;
    public CategoriesPage(CategoryViewModel vm)
    {
        this.vm = vm;
        Title = "Categories";
        BindingContext = vm;
        var name = new Entry
        {
            Placeholder = "New category name"
        };
        name.SetBinding(Entry.TextProperty, nameof(vm.NewName));
        var description = new Entry
        {
            Placeholder = "Category description"
        };
        description.SetBinding(Entry.TextProperty, nameof(vm.NewDescription));
        var status = new Label();
        status.SetBinding(Label.TextProperty, nameof(vm.ErrorMessage));
        var add = new Button
        {
            Text = "Add category"
        };
        var export = new Button
        {
            Text = "Export categories"
        };
        add.SetBinding(Button.CommandProperty, nameof(vm.AddCommand));
        export.SetBinding(Button.CommandProperty, nameof(vm.ExportCommand));
        var timestamp = new Label();
        timestamp.SetBinding(Label.TextProperty, nameof(vm.LastExportUtc), stringFormat: "Last export: {0:O}");
        var list = new CollectionView
        {
            ItemsSource = vm.Categories,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label();
                label.SetBinding(Label.TextProperty, nameof(Category.Name));
                return label;
            })
        };
        Content = new Grid
        {
            Padding = 16,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)],
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        name,
                        description,
                        add,
                        export,
                        status,
                        timestamp
                    }
                },
                list.AtRow(1)
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        active = true;
        var current = ++visit;
        try
        {
            var pending = new[]
            {
                vm.LoadCommand.ExecutionTask,
                vm.AddCommand.ExecutionTask,
                vm.ExportCommand.ExecutionTask
            };
            foreach (var operation in pending.OfType<Task>().Distinct())
                try
                {
                    await operation;
                }
                catch (OperationCanceledException)
                {
                }

            if (active && current == visit && vm.LoadCommand.CanExecute(null))
                await vm.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception)
        {
            if (active && current == visit)
                await DisplayAlertAsync("Loading unavailable", "Try reopening categories.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        active = false;
        visit++;
        vm.LoadCommand.Cancel();
        vm.AddCommand.Cancel();
        vm.ExportCommand.Cancel();
        base.OnDisappearing();
    }
}

public sealed class ExportSettings : IExportSettings
{
    public void SetLastExport(DateTimeOffset timestamp) => Preferences.Default.Set("LastExportUtc", timestamp.ToString("O"));
}

public sealed class NavigationActivityPage : ContentPage
{
    private readonly NavigationActivity state = new();
    private readonly Label status = new();
    public NavigationActivityPage()
    {
        Title = "Local checkout navigation";
        var total = new Entry
        {
            Placeholder = "Fictional total, for example 12.50",
            Keyboard = Keyboard.Numeric
        };
        var checkout = new Button
        {
            Text = "Checkout"
        };
        checkout.Clicked += async (_, _) =>
        {
            if (!state.TryCheckout(total.Text, out var error))
                await DisplayAlertAsync("Invalid total", error, "OK");
            Update();
        };
        var confirm = new Button
        {
            Text = "Confirm fictional order"
        };
        confirm.Clicked += async (_, _) =>
        {
            try
            {
                state.Confirm();
                Update();
            }
            catch (InvalidOperationException)
            {
                await DisplayAlertAsync("Checkout first", "Enter a valid total before confirming.", "OK");
            }
        };
        var back = new Button
        {
            Text = "Back in workflow"
        };
        back.Clicked += (_, _) =>
        {
            state.GoBack();
            Update();
        };
        var reset = new Button
        {
            Text = "Reset fictional workflow"
        };
        reset.Clicked += (_, _) =>
        {
            state.Reset();
            Update();
        };
        state.Restore(Preferences.Default.Get("activity_screen", "cart"));
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Local simulation only. No order is placed."
                },
                total,
                checkout,
                confirm,
                back,
                reset,
                status
            }
        };
        Update();
    }

    private void Update()
    {
        status.Text = $"Screen: {state.Screen}";
        Preferences.Default.Set("activity_screen", state.Screen);
    }
}

public sealed class ActivityGallery : ContentPage
{
    private readonly IServiceProvider services;
    public ActivityGallery(IServiceProvider services)
    {
        this.services = services;
        Title = "Chapter activities";
        var stack = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 8
        };
        Add(stack, "1–2 Controls and platform", () => new ControlsActivityPage());
        Add(stack, "3 Profile and theming", () => new ProfileEntryPage());
        Add(stack, "4 Adaptive settings", () => new AdaptiveSettingsPage());
        Add(stack, "5 Stock dashboard", () => new InventoryDashboardPage());
        Add(stack, "6 MVVM detail", () => services.GetRequiredService<ProductDetailActivityPage>());
        Add(stack, "7 Navigation state", () => services.GetRequiredService<CartPage>());
        Add(stack, "8 Category persistence and CSV", () => services.GetRequiredService<CategoriesPage>());
        Add(stack, "9 Offline queue", () => services.GetRequiredService<QueueActivityPage>());
        Add(stack, "11 Metric cards", () => new DashboardPage());
        Add(stack, "12 Shake-to-clear", () => new InspectionPage());
        Add(stack, "13 Controlled token renewal", () => new RenewalPracticePage());
        Add(stack, "14 Settings English", () => new LocalizedSettingsPage());
        Add(stack, "14 Configuración español", () => new LocalizedSettingsPage(true));
        Add(stack, "15 Browser lifetimes", () => new BrowsingPage(services.GetRequiredService<WeakMessageService>()));
        stack.Children.Add(new Label { Text = "Chapters 10 and 13 use the portable test project. Chapter 16 uses the version helper and target build commands. Real identity sign-in remains in Practice and requires your own registration." });
        Content = new ScrollView
        {
            Content = stack
        };
    }

    private void Add(VerticalStackLayout stack, string text, Func<Page> page)
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
                await DisplayAlertAsync("Activity unavailable", "Check its setup and target prerequisites.", "OK");
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        stack.Children.Add(button);
    }
}

public sealed class ControlsActivityPage : ContentPage
{
    public ControlsActivityPage()
    {
        Title = "Controls and platform";
#if ANDROID
        BackgroundColor=Color.FromArgb("#512BD4");
#else
        BackgroundColor = Color.FromArgb("#0078D4");
#endif
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = $"Platform: {DeviceInfo.Platform}; runtime {Environment.Version}",
                    TextColor = Colors.White
                },
                new BorderlessEntry
                {
                    Placeholder = "Custom entry"
                },
                new GradientButton
                {
                    Text = "Gradient marker button"
                },
                new QuantityPicker()
            }
        };
    }
}

public static class ActivityRoutes
{
    public static Page Create(int chapter, IServiceProvider services) => chapter switch
    {
        1 or 2 => new ControlsActivityPage(),
        3 => new ProfileEntryPage(),
        4 => new AdaptiveSettingsPage(),
        5 => new InventoryDashboardPage(),
        6 => services.GetRequiredService<ProductDetailActivityPage>(),
        7 => services.GetRequiredService<CartPage>(),
        8 => services.GetRequiredService<CategoriesPage>(),
        9 => services.GetRequiredService<QueueActivityPage>(),
        10 => Instructions("Testing activity", "Run the included Inventory.Tests project to exercise the fictional order workflow."),
        11 => new DashboardPage(),
        12 => new InspectionPage(),
        13 => new RenewalPracticePage(),
        14 => new LocalizedSettingsPage(),
        15 => new BrowsingPage(services.GetRequiredService<WeakMessageService>()),
        16 => Instructions("Release preparation", "Use the included update-version.ps1 on a disposable project copy. Signing, publishing and store actions require separate setup and authorization."),
        _ => throw new ArgumentOutOfRangeException(nameof(chapter))};
    private static Page Instructions(string title, string message) => new ContentPage
    {
        Title = title,
        Content = new Label
        {
            Text = message,
            Margin = 20
        }
    };
}
