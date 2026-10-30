using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class OrdersControllerTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Fact]
    public async Task CreateOrder_Returns201WithALocationInTheSameVersion()
    {
        var client = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-create");

        var response = await client.PostOrderAsync(NewOrder("ERP-T-100", 2));
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/v2/orders/{order!.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task CreateOrder_RetriedWithItsKey_ReturnsTheSameOrder_AndAnotherClientsReuseGets422()
    {
        var client = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-retry");
        var otherClient = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-other");
        var key = Guid.NewGuid().ToString();

        var first = await client.PostOrderAsync(NewOrder("ERP-T-107", 1), key);
        var retry = await client.PostOrderAsync(NewOrder("ERP-T-107", 1), key);
        var reuse = await otherClient.PostOrderAsync(NewOrder("ERP-T-107", 1), key);

        // The retry gets the stored order; the same key and body from another caller is another request
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        Assert.Equal(first.Headers.Location, retry.Headers.Location);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reuse.StatusCode);
    }

    [Fact]
    public async Task RequestShipment_ReservesStockBeforeTheResponse_AndRegistersTheShipmentAfterIt()
    {
        var order = await CreateOrderAsync("ERP-T-101", 4);
        var supervisor = factory.CreateClientAs(UserRoles.Supervisor);
        var before = await GetStockAsync(supervisor);

        var response = await supervisor.PostAsJsonAsync($"/api/v2/orders/{order.Id}/shipment", new AllocateOrderRequest("WH-001"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal($"/api/v2/orders/{order.Id}/shipment", response.Headers.Location!.AbsolutePath);
        Assert.Equal(before.Reserved + 4, (await GetStockAsync(supervisor)).Reserved);

        var shipment = await WaitForShipmentAsync(supervisor, order.Id, ShipmentStatus.Registered);
        Assert.StartsWith("SHP-TEST-", shipment.ShipmentId);
        Assert.Single(factory.Erp.IdempotencyKeys, key => key == $"order-{order.Id}");
    }

    [Fact]
    public async Task RequestShipment_RetriesErpFailuresWithTheSameIdempotencyKey()
    {
        var order = await CreateOrderAsync("ERP-T-102", 1);
        var supervisor = factory.CreateClientAs(UserRoles.Supervisor);
        factory.Erp.FailNext(2);

        await supervisor.PostAsJsonAsync($"/api/v2/orders/{order.Id}/shipment", new AllocateOrderRequest("WH-001"));
        var shipment = await WaitForShipmentAsync(supervisor, order.Id, ShipmentStatus.Registered);

        // One job attempt: the resilience handler retried the two 503 responses inside it
        Assert.Equal(1, shipment.Attempts);
        Assert.Equal(3, factory.Erp.IdempotencyKeys.Count(key => key == $"order-{order.Id}"));
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    public async Task Allocate_KeepsItsContractInEveryVersion(string version)
    {
        var order = await CreateOrderAsync($"ERP-T-103-{version}", 1);
        var supervisor = factory.CreateClientAs(UserRoles.Supervisor);

        var response = await supervisor.PostAsJsonAsync($"/api/{version}/orders/{order.Id}/allocate", new AllocateOrderRequest("WH-001"));
        var allocated = await response.Content.ReadFromJsonAsync<OrderResponse>();

        // 200 with the order, as in Chapter 14; the shipment is requested all the same
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Allocated, allocated!.Status);
        await WaitForShipmentAsync(supervisor, order.Id, ShipmentStatus.Registered);
    }

    [Fact]
    public async Task Allocate_AsWarehouseStaff_Returns403()
    {
        var order = await CreateOrderAsync("ERP-T-104", 1);
        var staff = factory.CreateClientAs(UserRoles.WarehouseStaff, "WH-001");

        var response = await staff.PostAsJsonAsync($"/api/v2/orders/{order.Id}/allocate", new AllocateOrderRequest("WH-001"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnotherIntegrationClient_DoesNotFindTheOrder()
    {
        var order = await CreateOrderAsync("ERP-T-105", 1);
        var otherClient = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "another-partner");

        var response = await otherClient.GetAsync($"/api/v2/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_AfterTheErpRegisteredTheShipment_Returns409()
    {
        var order = await CreateOrderAsync("ERP-T-106", 1);
        var supervisor = factory.CreateClientAs(UserRoles.Supervisor);
        await supervisor.PostAsJsonAsync($"/api/v2/orders/{order.Id}/shipment", new AllocateOrderRequest("WH-001"));
        await WaitForShipmentAsync(supervisor, order.Id, ShipmentStatus.Registered);

        var response = await supervisor.PostAsync($"/api/v2/orders/{order.Id}/cancel", content: null);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SHIPMENT_REGISTERED", problem.RootElement.GetProperty("conflictReason").GetString());
    }

    private static CreateOrderRequest NewOrder(string reference, int quantity) =>
        new(reference, null, [new OrderLineRequest("SKU-001", quantity)]);

    private async Task<OrderResponse> CreateOrderAsync(string reference, int quantity)
    {
        var client = factory.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: $"erp-{reference}");
        var response = await client.PostOrderAsync(NewOrder(reference, quantity));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<StockItemResponse> GetStockAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<StockItemResponse>("/api/v2/warehouses/WH-001/stock/SKU-001"))!;

    // The processor registers the shipment after the response: poll the status monitor, as a client would
    private static async Task<ShipmentResponse> WaitForShipmentAsync(HttpClient client, Guid orderId, ShipmentStatus status)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var shipment = await client.GetFromJsonAsync<ShipmentResponse>($"/api/v2/orders/{orderId}/shipment");
            if (shipment?.Status == status)
            {
                return shipment;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"The shipment for order {orderId} did not reach {status}.");
    }
}
