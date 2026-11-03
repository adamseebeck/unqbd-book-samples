using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Chapter 11's rules, tested without HTTP or a database.
[Trait("Category", "Unit")]
public sealed class OrderServiceTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);
    static readonly Product Tent = new(4, "Two-Person Tent",
        "TNT-4001", 189.00m, "camping", 1);

    readonly FakeOrders _orders = new();
    readonly FakeUnitOfWork _unitOfWork = new();
    readonly OrderService _service;

    public OrderServiceTests() => _service = new OrderService(
        _orders, _orders, new FakeCatalog(Helmet, Tent),
        _unitOfWork);

    static CreateOrderRequest Lines(
        params (int ProductId, int Quantity)[] lines) => new()
    {
        Items = [.. lines.Select(l => new OrderLineRequest
        {
            ProductId = l.ProductId, Quantity = l.Quantity
        })]
    };

    [Fact]
    public async Task PricesComeFromTheCatalog()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((1, 2), (4, 1)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Created, result.Outcome);
        Assert.Equal(248.98m, result.Order!.Total);
        Assert.Equal("casey", result.Order.OwnerId);
        Assert.Equal(1, _unitOfWork.Saves);
    }

    [Fact]
    public async Task OneLinePerProduct()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((1, 1), (1, 2)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Invalid, result.Outcome);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Fact]
    public async Task UnknownProductsAreRefused()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((99, 1)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Invalid, result.Outcome);
        Assert.Empty(_orders.Saved);
    }

    [Fact]
    public async Task ATakenNumberIsAConflict()
    {
        _unitOfWork.Failure = new OrderNumberConflictException(
            new InvalidOperationException("unique index"));

        var result = await _service.CreateAsync(
            "casey", Lines((1, 1)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Conflict, result.Outcome);
    }

    [Fact]
    public async Task AnotherOwnersOrderIsNotFound()
    {
        await _service.CreateAsync("casey", Lines((1, 1)),
            TestContext.Current.CancellationToken);

        var result = await _service.CancelAsync(
            "jordan", _orders.Saved[0].Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
        Assert.Null(_orders.StagedStatus);
    }
}
