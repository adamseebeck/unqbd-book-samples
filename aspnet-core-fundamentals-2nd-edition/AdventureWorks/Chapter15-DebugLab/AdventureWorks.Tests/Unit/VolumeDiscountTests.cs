using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Debugging lab: the volume discount starts at ten of a product.
[Trait("Category", "Unit")]
public sealed class VolumeDiscountTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);

    readonly FakeOrders _orders = new();

    OrderService Service() => new(_orders, _orders,
        new FakeCatalog(Helmet), new FakeUnitOfWork());

    static CreateOrderRequest Helmets(int quantity) => new()
    {
        Items = [new() { ProductId = 1, Quantity = quantity }]
    };

    [Fact]
    public async Task NineHelmetsPayTheListPrice()
    {
        var result = await Service().CreateAsync("casey",
            Helmets(9), TestContext.Current.CancellationToken);

        Assert.Equal(29.99m, result.Order!.Items[0].UnitPrice);
    }

    [Fact]
    public async Task TenHelmetsCostTenPercentLess()
    {
        var result = await Service().CreateAsync("casey",
            Helmets(10), TestContext.Current.CancellationToken);

        Assert.Equal(26.99m, result.Order!.Items[0].UnitPrice);
    }
}
