using System.Net;
using AdventureWorks.Core;
using AdventureWorks.Tests.Integration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AdventureWorks.Tests;

// Activity: what the service passes to the catalog, and a
// cancelled request that must save nothing.
[Trait("Category", "Unit")]
public sealed class OrderServiceMockTests
{
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IUnitOfWork> _unitOfWork = new();

    OrderService Service() => new(Mock.Of<IOrderRepository>(),
        Mock.Of<IOrderWriter>(), _catalog.Object,
        _unitOfWork.Object);

    static CreateOrderRequest OneHelmet() => new()
    {
        Items = [new() { ProductId = 1, Quantity = 1 }]
    };

    [Fact]
    public async Task TheCatalogGetsTheCallersToken()
    {
        using var request = new CancellationTokenSource();

        await Service().CreateAsync("casey", OneHelmet(),
            request.Token);

        _catalog.Verify(c => c.GetByIdsAsync(
            It.Is<int[]>(ids => ids.SequenceEqual(new[] { 1 })),
            request.Token), Times.Once);
    }

    [Fact]
    public async Task ACancelledRequestSavesNothing()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        using var request = new CancellationTokenSource();
        await request.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service().CreateAsync(
                "casey", OneHelmet(), request.Token));

        _unitOfWork.Verify(u => u.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

// Activity: WithWebHostBuilder changes one test's app only; the
// class's shared factory keeps the real InventoryClient.
[Trait("Category", "Integration")]
public sealed class StockOverrideTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task AFailingWarehouseIs503()
    {
        var stub = new StubInventory(
            HttpStatusCode.ServiceUnavailable);
        using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<InventoryClient>()
                    .ConfigurePrimaryHttpMessageHandler(
                        () => stub)));

        var response = await failing.CreateClient().GetAsync(
            "/api/catalog/4/availability",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        // One call and Chapter 12's two retries.
        Assert.Equal(3, stub.Calls);
    }
}

// Answers InventoryClient's calls in place of InventoryService
// with one status, and counts them.
public sealed class StubInventory(HttpStatusCode status)
    : HttpMessageHandler
{
    private int _calls;

    public int Calls => _calls;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(new HttpResponseMessage(status));
    }
}
