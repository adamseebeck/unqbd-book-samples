using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WarehouseApi.HealthChecks;
using WarehouseApi.Integration;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests;

public class ErpIntegrationHealthCheckTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Theory]
    [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HealthStatus.Degraded)]
    [InlineData(HttpStatusCode.InternalServerError, HealthStatus.Unhealthy)]
    public async Task MapsTheErpAnswerToAHealthStatus(HttpStatusCode erpStatus, HealthStatus expected)
    {
        var result = await CheckAsync(new FakeErpHealthHandler(_ => Task.FromResult(new HttpResponseMessage(erpStatus))));

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task ReportsUnhealthy_WhenTheErpCannotBeReached()
    {
        var result = await CheckAsync(new FakeErpHealthHandler(_ => throw new HttpRequestException("Connection refused")));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ReportsDegraded_WhenTheErpTakesLongerThanFiveSeconds()
    {
        var result = await CheckAsync(new FakeErpHealthHandler(async token =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task TheDependenciesEndpointReportsTheErp_WhileReadinessStaysHealthy()
    {
        await using var erpDown = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient(ErpIntegrationHealthCheck.ClientName)
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new FakeErpHealthHandler(_ => throw new HttpRequestException("Connection refused")))));
        var client = erpDown.CreateClient();

        var dependencies = await client.GetAsync("/health/dependencies");
        var readiness = await client.GetAsync("/health");
        using var report = JsonDocument.Parse(await dependencies.Content.ReadAsStringAsync());
        var erp = report.RootElement.GetProperty("checks").EnumerateArray().Single();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, dependencies.StatusCode);
        Assert.Equal("erp-integration", erp.GetProperty("name").GetString());
        Assert.Equal("Unhealthy", erp.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
    }

    private static Task<HealthCheckResult> CheckAsync(HttpMessageHandler handler)
    {
        var check = new ErpIntegrationHealthCheck(
            new SingleHandlerClientFactory(handler),
            Options.Create(new ExternalShipmentServiceOptions { BaseUrl = "https://erp.example.com/", ApiKey = "test-key" }));

        return check.CheckHealthAsync(new HealthCheckContext());
    }

    // Hand-written test doubles: MockHttpMessageHandler is not part of .NET
    private sealed class FakeErpHealthHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(cancellationToken);
    }

    private sealed class SingleHandlerClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
