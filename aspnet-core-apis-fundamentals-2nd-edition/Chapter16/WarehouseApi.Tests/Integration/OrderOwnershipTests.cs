using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Models;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class OrderOwnershipTests(OrderOwnershipFactory factory) : IClassFixture<OrderOwnershipFactory>
{
    public static IEnumerable<object[]> OrderRoutes() =>
        [["/api/orders"], ["/api/v1/orders"], ["/api/v2/orders"]];

    public static IEnumerable<object[]> DistinctSubjects()
    {
        string[] routes = ["/api/orders", "/api/v1/orders", "/api/v2/orders"];
        (string Owner, string Other)[] subjects =
        [
            ("PartnerA", "partnera"),
            ("PartnerSpace", "PartnerSpace "),
            ("PartnerSpace ", "PartnerSpace  "),
            ("PartnerCafe", "PartnerCaf\u00e9"),
            ("PartnerA", "Partner\uff21"),
            ("Partner\u30ab", "Partner\u304b"),
            ("Partner\u00e9", "Partnere\u0301")
        ];
        foreach (var route in routes)
        {
            foreach (var (owner, other) in subjects)
            {
                yield return [route, owner, other];
            }
        }
    }

    [Theory]
    [MemberData(nameof(DistinctSubjects))]
    public async Task ExactSubjectOwnsItsOrder_AndDifferentSubjectsCannotReadOrReplayIt(
        string route, string ownerSubject, string otherSubject)
    {
        // A fresh namespace keeps every case independent without trimming either subject.
        var prefix = Guid.NewGuid().ToString("N") + ":";
        using var owner = factory.CreateCaller(UserRoles.IntegrationClient, prefix + ownerSubject, "WH-001");
        using var other = factory.CreateCaller(UserRoles.IntegrationClient, prefix + otherSubject, "WH-001");
        var key = Guid.NewGuid().ToString();
        var body = new CreateOrderRequest("SUBJECT-" + key[..8], "WH-001", [new("SKU-001", 1)]);

        using var created = await PostOrderAsync(owner, route, body, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderResponse>())!;

        using var ownRead = await owner.GetAsync($"{route}/{order.Id}");
        using var foreignRead = await other.GetAsync($"{route}/{order.Id}");
        Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);

        using var ownList = await owner.GetAsync(route);
        using var foreignList = await other.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, ownList.StatusCode);
        Assert.Equal(HttpStatusCode.OK, foreignList.StatusCode);
        Assert.Contains(order.Id.ToString(), await ownList.Content.ReadAsStringAsync());
        Assert.DoesNotContain(order.Id.ToString(), await foreignList.Content.ReadAsStringAsync());
        if (route == "/api/v2/orders")
        {
            using var page = JsonDocument.Parse(await foreignList.Content.ReadAsStringAsync());
            Assert.Equal(0, page.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Empty(page.RootElement.GetProperty("items").EnumerateArray());
        }

        using var ownRetry = await PostOrderAsync(owner, route, body, key);
        using var foreignRetry = await PostOrderAsync(other, route, body, key);
        Assert.Equal(HttpStatusCode.Created, ownRetry.StatusCode);
        Assert.Equal(created.Headers.Location, ownRetry.Headers.Location);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreignRetry.StatusCode);

        // Shipment monitoring must use the same ownership boundary as the order.
        await using var context = factory.CreateDbContext();
        if (route == "/api/v2/orders")
        {
            context.Shipments.Add(new Shipment
            {
                OrderId = order.Id,
                Status = ShipmentStatus.Registered,
                ShipmentId = "SUBJECT-SHIPMENT",
                CorrelationId = key,
                RequestedAt = DateTime.UtcNow,
                RegisteredAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            using var ownShipment = await owner.GetAsync($"{route}/{order.Id}/shipment");
            using var foreignShipment = await other.GetAsync($"{route}/{order.Id}/shipment");
            Assert.Equal(HttpStatusCode.OK, ownShipment.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, foreignShipment.StatusCode);
        }
    }

    [Theory]
    [InlineData("WH-001")]
    [InlineData("wh-001")]
    public async Task StaffAndSupervisorsKeepTheirExistingReadAccess(string warehouse)
    {
        var reference = "STAFF-" + Guid.NewGuid().ToString("N")[..8];
        using var owner = factory.CreateCaller(UserRoles.IntegrationClient, reference, "WH-001");
        using var created = await PostOrderAsync(owner, "/api/orders",
            new(reference, "WH-001", [new("SKU-001", 1)]), Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderResponse>())!;

        using var staff = factory.CreateCaller(UserRoles.WarehouseStaff, null, warehouse);
        using var supervisor = factory.CreateCaller(UserRoles.Supervisor, null);
        using var mixed = factory.CreateCaller($"{UserRoles.WarehouseStaff},{UserRoles.IntegrationClient}", null, warehouse);
        using var outsideStaff = factory.CreateCaller(UserRoles.WarehouseStaff, null, "WH-002");
        foreach (var reader in new[] { staff, supervisor, mixed })
        {
            using var response = await reader.GetAsync($"/api/orders/{order.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var outside = await outsideStaff.GetAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task IntegrationClientWithoutASubjectCannotCreateOrReplayAnOrder(string? subject)
    {
        var key = Guid.NewGuid().ToString();
        var body = new CreateOrderRequest("NO-SUBJECT", "WH-001", [new("SKU-001", 1)]);
        using var supervisor = factory.CreateCaller(UserRoles.Supervisor, null);
        using var created = await PostOrderAsync(supervisor, "/api/orders", body, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var client = factory.CreateCaller(UserRoles.IntegrationClient, subject, "WH-001");
        using var replay = await PostOrderAsync(client, "/api/orders", body, key);
        using var fresh = await PostOrderAsync(client, "/api/orders", body, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, fresh.StatusCode);
    }

    [Theory]
    [MemberData(nameof(OrderRoutes))]
    public async Task EmptySubjectCannotReadAnOrderWithAnEmptyCreator(string route)
    {
        using var supervisor = factory.CreateCaller(UserRoles.Supervisor, "");
        var key = Guid.NewGuid().ToString();
        using var created = await PostOrderAsync(supervisor, route,
            new("EMPTY-OWNER", "WH-001", [new("SKU-001", 1)]), key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderResponse>())!;

        foreach (var subject in new string?[] { null, "" })
        {
            using var client = factory.CreateCaller(UserRoles.IntegrationClient, subject, "WH-001");
            using var read = await client.GetAsync($"{route}/{order.Id}");
            using var list = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var body = await list.Content.ReadAsStringAsync();
            Assert.DoesNotContain(order.Id.ToString(), body);
            if (route == "/api/v2/orders")
            {
                using var page = JsonDocument.Parse(body);
                Assert.Equal(0, page.RootElement.GetProperty("totalCount").GetInt32());
                Assert.Empty(page.RootElement.GetProperty("items").EnumerateArray());
            }
        }
    }

    [Fact]
    public async Task EmptySubjectCannotMonitorAnEmptyOwnedShipment()
    {
        using var supervisor = factory.CreateCaller(UserRoles.Supervisor, "");
        var key = Guid.NewGuid().ToString();
        using var created = await PostOrderAsync(supervisor, "/api/v2/orders",
            new("EMPTY-SHIPMENT", "WH-001", [new("SKU-001", 1)]), key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderResponse>())!;
        await using var context = factory.CreateDbContext();
        context.Shipments.Add(new Shipment
        {
            OrderId = order.Id, Status = ShipmentStatus.Registered,
            ShipmentId = "EMPTY-OWNER-SHIPMENT", CorrelationId = key,
            RequestedAt = DateTime.UtcNow, RegisteredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        using var ownerRead = await supervisor.GetAsync($"/api/v2/orders/{order.Id}/shipment");
        Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);
        using var client = factory.CreateCaller(UserRoles.IntegrationClient, "", "WH-001");
        using var read = await client.GetAsync($"/api/v2/orders/{order.Id}/shipment");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Theory]
    [MemberData(nameof(OrderRoutes))]
    public async Task StockQueriesKeepTheirWarehouseBasedAuthorization(string orderRoute)
    {
        var stockRoute = orderRoute.Replace("/orders", "/stock");
        foreach (var subject in new string?[] { null, "" })
        {
            using var client = factory.CreateCaller(UserRoles.IntegrationClient, subject, "WH-001");
            using var permitted = await client.GetAsync($"{stockRoute}?warehouseCode=WH-001");
            using var forbidden = await client.GetAsync($"{stockRoute}?warehouseCode=WH-002");
            Assert.Equal(HttpStatusCode.OK, permitted.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> PostOrderAsync(
        HttpClient client, string route, CreateOrderRequest body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }
}
