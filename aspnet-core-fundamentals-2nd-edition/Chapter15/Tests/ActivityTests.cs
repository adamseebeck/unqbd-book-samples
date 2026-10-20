using System.Net;
using System.Net.Http.Json;
using AdventureWorks.Domain;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

public sealed class ActivityTests
{
    [Fact, Trait("Category", "Unit")]
    public async Task AvailableOrderPersistsWithToken()
    {
        using var source = new CancellationTokenSource();
        var inventory = new Mock<IInventoryService>();
        var repository = new Mock<IOrderRepository>();
        inventory.Setup(i => i.IsAvailableAsync(1, 5, source.Token)).ReturnsAsync(true);
        repository.Setup(r => r.AddAsync(It.IsAny<Order>(), source.Token)).Returns(Task.CompletedTask);
        var order = await new OrderService(inventory.Object, repository.Object)
            .CreateAsync(new(1, 5), source.Token);
        Assert.Equal(5, order.Quantity);
        repository.Verify(r => r.AddAsync(It.Is<Order>(o => o.Quantity == 5), source.Token), Times.Once);
    }

    [Fact, Trait("Category", "Unit")]
    public async Task CancellationDoesNotPersist()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var inventory = new Mock<IInventoryService>();
        var repository = new Mock<IOrderRepository>();
        inventory.Setup(i => i.IsAvailableAsync(1, 5, source.Token))
            .Returns(Task.FromCanceled<bool>(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new OrderService(inventory.Object, repository.Object).CreateAsync(new(1, 5), source.Token));
        repository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task InventoryOverrideStaysInsideDerivedFactory()
    {
        using var factory = new AdventureWorksFactory();
        using var normal = factory.CreateInitializedClient();
        var inventory = new Mock<IInventoryService>();
        inventory.Setup(i => i.IsAvailableAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var overridden = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IInventoryService>();
            services.AddSingleton(inventory.Object);
        }));
        using var special = overridden.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await special.PostAsJsonAsync("/api/orders", new CreateOrderRequest(1, 20))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await normal.PostAsJsonAsync("/api/orders", new CreateOrderRequest(1, 20))).StatusCode);
    }
}
