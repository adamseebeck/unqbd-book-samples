using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Security;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class OrderInputTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Theory]
    [InlineData("v1", false)]
    [InlineData("v1", true)]
    [InlineData("v2", false)]
    [InlineData("v2", true)]
    public async Task NullLine_Returns400WithoutCreatingAnOrderOrReplay(string version, bool includeValidLine)
    {
        var user = $"null-line-{version}-{includeValidLine}";
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, UserRoles.IntegrationClient);
        client.DefaultRequestHeaders.Add(TestAuthHandler.WarehouseHeader, "WH-001");
        var lines = includeValidLine ? "[{\"sku\":\"SKU-001\",\"quantity\":1},null]" : "[null]";
        var json = "{\"customerReference\":\"NULL-LINE\",\"lines\":" + lines + "}";
        var key = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/{version}/orders")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", key);

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var field = includeValidLine ? "Lines[1]" : "Lines[0]";
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));

        await using var database = factory.CreateDbContext();
        Assert.False(await database.Orders.AnyAsync(order => order.CreatedBy == user));
        Assert.False(await database.IdempotencyRecords.AnyAsync(record => record.Key == key));
    }
}
