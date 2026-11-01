using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

public static class Chapter10Lab
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
        string[] paths = ["/health/live", "/health/ready"];
        foreach (var path in paths)
        {
            var response = await http.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"GET {path}: {(int)response.StatusCode} {body}");
        }

        Guid firstId;
        using (var scope = services.CreateScope())
        {
            var id = scope.GetContext().ContextId;
            (firstId, var lease) = (id.InstanceId, id.Lease);
            Console.WriteLine($"First request: lease {lease}");
        }
        using (var scope = services.CreateScope())
        {
            var id = scope.GetContext().ContextId;
            var same = id.InstanceId == firstId;
            Console.WriteLine(
                $"Second request: lease {id.Lease}");
            Console.WriteLine($"Same pooled context: {same}");
        }
    }
}
