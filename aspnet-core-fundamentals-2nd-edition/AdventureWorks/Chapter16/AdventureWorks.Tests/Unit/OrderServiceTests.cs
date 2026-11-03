using AdventureWorks.Core;
using Moq;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Chapter 11's rules without HTTP or a database. Moq stands in
// for the four contracts OrderService depends on.
[Trait("Category", "Unit")]
public sealed class OrderServiceTests
{
    readonly Mock<IOrderRepository> _orders = new();
    readonly Mock<IOrderWriter> _writer = new();
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IUnitOfWork> _unitOfWork = new();

    OrderService Service() => new(_orders.Object, _writer.Object,
        _catalog.Object, _unitOfWork.Object);

    [Fact]
    public async Task PricesComeFromTheCatalog()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product(4, "Two-Person Tent",
                "TNT-4001", 189.00m, "camping", 1)]);
        NewOrder? staged = null;
        _writer.Setup(w => w.Add(It.IsAny<NewOrder>()))
            .Callback((NewOrder order) => staged = order);

        await Service().CreateAsync("casey", new()
        {
            Items = [new() { ProductId = 4, Quantity = 2 }]
        }, TestContext.Current.CancellationToken);

        Assert.Equal(189.00m, staged!.Items[0].UnitPrice);
        Assert.Equal("casey", staged.OwnerId);
    }

    // Deleted after the service read it: the save finds no row.
    [Fact]
    public async Task AnOrderDeletedBeforeTheSaveIsNotFound()
    {
        _orders.Setup(o => o.GetByIdAsync(7)).ReturnsAsync(
            new Order(7, "AW-0007", "casey", 29.99m,
                OrderStatus.Pending, false, null, Guid.NewGuid(),
                []));
        _writer.Setup(w => w.SetStatusAsync(7, It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWork.Setup(u => u.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OrderDeletedException(new()));

        var result = await Service().CancelAsync("casey", 7,
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
        // Reached the save: not the owner or staging checks.
        _unitOfWork.Verify(u => u.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
