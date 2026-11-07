using AdventureWorks.Core;
using Moq;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Debugging lab: the volume discount starts at ten of a product.
[Trait("Category", "Unit")]
public sealed class VolumeDiscountTests
{
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IOrderWriter> _writer = new();
    NewOrder? _staged;

    public VolumeDiscountTests()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product(1, "Trail Helmet",
                "HLM-1001", 29.99m, "accessories", 1)]);
        _writer.Setup(w => w.Add(It.IsAny<NewOrder>()))
            .Callback((NewOrder order) => _staged = order);
    }

    // The unit price OrderService staged for that many helmets.
    async Task<decimal> UnitPriceFor(int helmets)
    {
        var service = new OrderService(
            Mock.Of<IOrderRepository>(), _writer.Object,
            _catalog.Object, Mock.Of<IUnitOfWork>());
        await service.CreateAsync("casey", new()
        {
            Items = [new() { ProductId = 1, Quantity = helmets }]
        }, TestContext.Current.CancellationToken);
        return _staged!.Items[0].UnitPrice;
    }

    [Fact]
    public async Task NineHelmetsPayTheListPrice() =>
        Assert.Equal(29.99m, await UnitPriceFor(9));

    [Fact]
    public async Task TenHelmetsCostTenPercentLess() =>
        Assert.Equal(26.99m, await UnitPriceFor(10));
}
