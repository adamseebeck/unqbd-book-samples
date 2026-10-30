using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WarehouseApi.Services;

namespace WarehouseApi.Tests;

public class ErrorResponseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid OrderId = Guid.Parse("7d1f3a52-9c4e-4b8a-a0f6-2e5d8c9b1a47");
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorResponseTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Production: no seeding, and the error responses a real client receives
            builder.UseEnvironment("Production");

            // AddPersistence requires a connection string; these tests never open a connection
            builder.UseSetting("ConnectionStrings:WarehouseDb", "Server=(localdb)\\MSSQLLocalDB;Database=WarehouseApiTests;Integrated Security=true");

            builder.ConfigureTestServices(services =>
                services.AddScoped<IOrderService, ThrowingOrderService>());
        });
    }

    [Fact]
    public async Task MissingOrder_ReturnsProblemDetailsWithTheFieldsTheHandlerAlwaysSends()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Orders/{OrderId}/cancel");
        request.Headers.Add("X-Correlation-ID", "test-404");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = body.RootElement;

        // RFC 9457 makes every member optional; this API always sends these
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", problem.GetProperty("type").GetString());
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Contains(OrderId.ToString(), problem.GetProperty("detail").GetString());
        Assert.Equal($"/api/Orders/{OrderId}/cancel", problem.GetProperty("instance").GetString());
        Assert.Equal("test-404", problem.GetProperty("correlationId").GetString());
        Assert.Equal("Order", problem.GetProperty("resourceType").GetString());
    }

    [Fact]
    public async Task ConcurrencyConflict_Returns409WithThePublicIdentifiers()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/Orders/{OrderId}/fulfill", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = body.RootElement;

        Assert.Equal("https://api.example.com/errors/concurrent-modification", problem.GetProperty("type").GetString());
        Assert.Equal("CONCURRENT_MODIFICATION", problem.GetProperty("conflictReason").GetString());
        Assert.Equal("StockItem", problem.GetProperty("resourceType").GetString());
        Assert.Equal("WH-001/SKU-001", problem.GetProperty("resourceId").GetString());
        Assert.True(problem.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task UnexpectedFailure_IsSanitizedOutsideDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/Orders/{OrderId}");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("sql-prod-01", text);

        using var body = JsonDocument.Parse(text);
        var problem = body.RootElement;

        Assert.Equal("An unexpected error occurred processing your request.", problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("stackTrace", out _));
        Assert.True(problem.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task ClientThatAcceptsOnlyText_GetsAPlainTextAnswer()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Orders/{OrderId}/cancel");
        request.Headers.Add("Accept", "text/plain");
        request.Headers.Add("X-Correlation-ID", "test-text");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Not Found: correlation ID test-text", await response.Content.ReadAsStringAsync());
    }
}
