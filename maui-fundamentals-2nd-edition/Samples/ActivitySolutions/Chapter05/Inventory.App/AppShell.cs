using Microsoft.Extensions.DependencyInjection;

namespace MauiInventory.App;
public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        Routing.RegisterRoute("details", typeof(DetailsPage));

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
    }
}
