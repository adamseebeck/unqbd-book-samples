using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

public static class Chapter11Activity
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
            await RetailDatabase.ResetAsync(
                scope.GetContext(), Seed.CatalogAsync);

        var address = services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();
        using var http = new HttpClient
        {
            BaseAddress = new Uri(address)
        };

        var lamp = new CreateProduct("Desk Lamp", "LAMP-004",
            24.00m, 7, CategoryId: 2);
        var created = await http.PostAsJsonAsync(
            "/products", lamp);
        var location = created.Headers.Location!.ToString();
        Console.WriteLine(
            $"POST /products: {(int)created.StatusCode} " +
            location);

        var read = await http.GetAsync(location);
        var view = await read.Content
            .ReadFromJsonAsync<ProductView>();
        Console.WriteLine(
            $"GET {location}: {(int)read.StatusCode} " +
            view!.Name);

        var edit = new EditProduct(view.Name, 26.00m,
            view.StockQuantity, view.Version);
        await PutAsync(http, location, edit, "fresh version");
        await PutAsync(http, location, edit, "old version");

        var again = await http.PostAsJsonAsync(
            "/products", lamp);
        Console.WriteLine(
            $"POST duplicate SKU: {(int)again.StatusCode}");
        var unknown = await http.PostAsJsonAsync("/products",
            lamp with { Sku = "LAMP-005", CategoryId = 99 });
        Console.WriteLine(
            $"POST unknown category: {(int)unknown.StatusCode}");

        using var check = services.CreateScope();
        var count = await check.GetContext()
            .Products.CountAsync();
        Console.WriteLine($"Products: {count}");
    }

    private static async Task PutAsync(HttpClient http,
        string location, EditProduct edit, string label)
    {
        var response = await http.PutAsJsonAsync(location, edit);
        Console.WriteLine(
            $"PUT ({label}): {(int)response.StatusCode}");
    }
}
