using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class AppShell : Shell
{
    private readonly WorkflowState workflow;
    public AppShell(IServiceProvider services)
    {
        workflow = services.GetRequiredService<WorkflowState>();
        Routing.RegisterRoute("details", typeof(DetailsPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
        Routing.RegisterRoute("productdetail", typeof(ProductDetailActivityPage));
        Routing.RegisterRoute("checkout", typeof(CheckoutPage));
        Routing.RegisterRoute("confirmation", typeof(ConfirmationPage));
        var activity = typeof(AppShell).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "BookActivity")?.Value;
        if (int.TryParse(activity, out var chapter) && chapter is >= 1 and <= 16)
        {
            var item = new FlyoutItem
            {
                Title = $"Chapter {chapter} activity",
                Route = "activity"
            };
            item.Items.Add(new ShellContent { ContentTemplate = new DataTemplate(() => ActivityRoutes.Create(chapter, services)), Route = "exercise" });
            Items.Add(item);
            return;
        }

        var home = new FlyoutItem
        {
            Title = "Inventory",
            Route = "inventory"
        };
        var catalog = new Tab
        {
            Title = "Catalog",
            Route = "catalog"
        };
        catalog.Items.Add(new ShellContent { Title = "Products", Route = "products", ContentTemplate = new DataTemplate(() => services.GetRequiredService<ProductsPage>()) });
        var tools = new Tab
        {
            Title = "Practice",
            Route = "practice"
        };
        tools.Items.Add(new ShellContent { Title = "Chapter practice", Route = "tools", ContentTemplate = new DataTemplate(() => new PracticePage(services)) });
        home.Items.Add(catalog);
        home.Items.Add(tools);
        Items.Add(home);
        var settings = new FlyoutItem
        {
            Title = "Settings",
            Route = "settings"
        };
        settings.Items.Add(new ShellContent { ContentTemplate = new DataTemplate(typeof(SettingsPage)), Route = "preferences" });
        Items.Add(settings);
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);
        var fromConfirmation = args.Current?.Location?.OriginalString.TrimEnd('/').EndsWith("/confirmation", StringComparison.Ordinal) == true;
        if (workflow.Completed && !workflow.Resetting && fromConfirmation && args.CanCancel && (args.Source is ShellNavigationSource.Pop or ShellNavigationSource.PopToRoot))
            args.Cancel();
    }
}
