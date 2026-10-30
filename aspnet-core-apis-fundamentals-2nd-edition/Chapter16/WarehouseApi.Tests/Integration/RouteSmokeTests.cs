using System.Net;
using System.Net.Http.Json;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

// Two endpoints that match the same request start without error and fail every matching request
// with 500, so request each endpoint once instead of only checking that the application starts
public class RouteSmokeTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    private const string UnknownOrder = "7d1f3a52-9c4e-4b8a-a0f6-2e5d8c9b1a47";

    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", "/api/products" },
        { "GET", "/api/v1/products" },
        { "GET", "/api/v2/products" },
        { "POST", "/api/v2/products" },
        { "GET", "/api/v1/products/SKU-001" },
        { "GET", "/api/v2/products/SKU-001" },
        { "PUT", "/api/v2/products/SKU-999" },
        { "GET", "/api/v1/warehouses" },
        { "GET", "/api/v1/warehouses/1" },
        { "GET", "/api/v2/warehouses" },
        { "GET", "/api/v2/warehouses/WH-001" },
        { "GET", "/api/v2/warehouses/WH-001/stock" },
        { "GET", "/api/v2/warehouses/WH-001/stock/SKU-001" },
        { "POST", "/api/v2/warehouses/WH-001/stock/SKU-001/adjustments" },
        { "PUT", "/api/v2/warehouses/WH-002/stock/SKU-999" },
        { "DELETE", "/api/v2/warehouses/WH-002/stock/SKU-999" },
        { "GET", "/api/v1/stock" },
        { "GET", "/api/v2/stock" },
        { "GET", "/api/orders" },
        { "GET", "/api/v1/orders" },
        { "GET", "/api/v2/orders" },
        { "POST", "/api/v2/orders" },
        { "GET", $"/api/v1/orders/{UnknownOrder}" },
        { "GET", $"/api/v2/orders/{UnknownOrder}" },
        { "GET", $"/api/v2/orders/{UnknownOrder}/shipment" },
        { "POST", $"/api/v2/orders/{UnknownOrder}/shipment" },
        { "POST", $"/api/v1/orders/{UnknownOrder}/allocate" },
        { "POST", $"/api/v2/orders/{UnknownOrder}/allocate" },
        { "POST", $"/api/v2/orders/{UnknownOrder}/fulfill" },
        { "POST", $"/api/v2/orders/{UnknownOrder}/cancel" },
        { "DELETE", $"/api/v2/orders/{UnknownOrder}" },
        { "GET", "/health" },
        { "GET", "/health/live" }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EveryEndpoint_AnswersWithoutAServerError(string method, string path)
    {
        var client = factory.CreateClientAs(UserRoles.Supervisor);
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        // A body for the routes that require one; unknown IDs then give 404 rather than 400
        if (method is "PUT" or "POST")
        {
            request.Content = path.Contains("/stock/")
                ? JsonContent.Create(new { onHand = 5, reserved = 0, reorderThreshold = 1 })
                : path.EndsWith("/allocate") || path.EndsWith("/shipment")
                    ? JsonContent.Create(new { warehouseCode = "WH-001" })
                    : JsonContent.Create(new { name = "Unknown", description = (string?)null, unitPrice = 1.5m, isActive = true });
        }

        var response = await client.SendAsync(request);

        Assert.True((int)response.StatusCode < 500, $"{method} {path} answered {(int)response.StatusCode}");
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
