using Microsoft.Extensions.DependencyInjection;

namespace MauiInventory.App;
public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        var home = new FlyoutItem
        {
            Title = "Inventory",
            Route = "inventory"
        };
        var tools = new Tab
        {
            Title = "Practice",
            Route = "practice"
        };
        tools.Items.Add(new ShellContent { Title = "Chapter practice", Route = "tools", ContentTemplate = new DataTemplate(() => new PracticePage(services)) });
        home.Items.Add(tools);
        Items.Add(home);
    }
}
