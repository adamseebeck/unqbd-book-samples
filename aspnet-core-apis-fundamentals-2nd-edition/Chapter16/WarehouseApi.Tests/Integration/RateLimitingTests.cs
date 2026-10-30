using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class RateLimitingTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Fact]
    public async Task OrdersBeyondTheLimit_Get429WithRetryAfter()
    {
        // A second host with a lower limit; it shares the factory's database
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:OrderCreation:PermitLimit", "2"));
        var client = limited.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-burst");

        for (var i = 1; i <= 2; i++)
        {
            var accepted = await client.PostOrderAsync(NewOrder($"ERP-R-{i}"));
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }

        // The limiter runs before the action, so a request counts whatever its key
        var rejected = await client.PostOrderAsync(NewOrder("ERP-R-3"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        var retryAfter = rejected.Headers.RetryAfter?.Delta;
        Assert.NotNull(retryAfter);
        Assert.InRange(retryAfter.Value.TotalSeconds, 1, 60);
    }

    [Fact]
    public async Task EachClient_HasItsOwnBudget()
    {
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:OrderCreation:PermitLimit", "1"));
        var first = limited.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-north");
        var second = limited.CreateClientAs(UserRoles.IntegrationClient, "WH-001", user: "erp-south");

        Assert.Equal(HttpStatusCode.Created, (await first.PostOrderAsync(NewOrder("ERP-N-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await first.PostOrderAsync(NewOrder("ERP-N-2"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await second.PostOrderAsync(NewOrder("ERP-S-1"))).StatusCode);
    }

    private static CreateOrderRequest NewOrder(string reference) =>
        new(reference, null, [new OrderLineRequest("SKU-001", 1)]);
}
