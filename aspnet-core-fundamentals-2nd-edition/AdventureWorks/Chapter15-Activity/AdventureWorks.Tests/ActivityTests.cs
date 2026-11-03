using System.Net;
using AdventureWorks.Core;
using AdventureWorks.Tests.Integration;
using AdventureWorks.Tests.Unit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AdventureWorks.Tests;

// Activity: a mock that checks how the service calls the catalog,
// and a cancelled request that must save nothing.
[Trait("Category", "Unit")]
public sealed class OrderServiceMockTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);

    readonly Mock<IProductService> _catalog = new();
    readonly FakeOrders _orders = new();
    readonly FakeUnitOfWork _unitOfWork = new();

    OrderService Service() => new(_orders, _orders,
        _catalog.Object, _unitOfWork);

    static CreateOrderRequest OneHelmet() => new()
    {
        Items = [new() { ProductId = 1, Quantity = 1 }]
    };

    [Fact]
    public async Task TheCatalogGetsTheCallersToken()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Helmet]);
        using var request = new CancellationTokenSource();

        var result = await Service().CreateAsync(
            "casey", OneHelmet(), request.Token);

        Assert.Equal(OrderOutcome.Created, result.Outcome);
        _catalog.Verify(c => c.GetByIdsAsync(
            It.Is<int[]>(ids => ids.SequenceEqual(new[] { 1 })),
            request.Token), Times.Once);
    }

    [Fact]
    public async Task ACancelledRequestSavesNothing()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .Returns((int[] _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new[] { Helmet });
            });
        using var request = new CancellationTokenSource();
        await request.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service().CreateAsync(
                "casey", OneHelmet(), request.Token));

        Assert.Equal(0, _unitOfWork.Saves);
        Assert.Empty(_orders.Saved);
    }
}

// Activity: WithWebHostBuilder changes one test's app only.
[Trait("Category", "Integration")]
public sealed class StockOverrideTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task AnOverrideStaysInItsOwnTest()
    {
        using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<InventoryClient>()
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new StubInventory
                        {
                            Status =
                                HttpStatusCode.ServiceUnavailable
                        })));
        var ct = TestContext.Current.CancellationToken;

        var changed = await failing.CreateClient().GetAsync(
            "/api/catalog/4/availability", ct);
        var normal = await factory.CreateClient().GetAsync(
            "/api/catalog/4/availability", ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, normal.StatusCode);
    }
}
