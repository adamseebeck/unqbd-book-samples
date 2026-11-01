using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

public static class Chapter11Lab
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

        var list = await http.GetFromJsonAsync<List<ProductView>>(
            "/products?take=2") ?? [];
        Console.WriteLine("GET /products?take=2: " +
            string.Join(", ", list.Select(p => p.Name)));

        var page = await http.GetFromJsonAsync<PageView>(
            "/products/page?page=2&take=2");
        Console.WriteLine(
            $"Page {page!.Page} of {page.TotalPages}: " +
            string.Join(", ", page.Items.Select(p => p.Name)));

        var first = list[0];
        var edit = new EditProduct(first.Name, 13.25m,
            first.StockQuantity, first.Version);
        await PutAsync(http, first.Id, edit, "fresh version");
        await PutAsync(http, first.Id, edit, "stale version");
        await PutAsync(http, first.Id, edit with { Price = -1 },
            "negative price");
        await PutAsync(http, 999, edit, "unknown id");
    }

    private static async Task PutAsync(HttpClient http, int id,
        EditProduct edit, string label)
    {
        var response = await http.PutAsJsonAsync(
            $"/products/{id}", edit);
        Console.WriteLine(
            $"PUT ({label}): {(int)response.StatusCode}");
    }

    private sealed record PageView(int Page, int Total,
        int TotalPages, List<ProductView> Items);
}
