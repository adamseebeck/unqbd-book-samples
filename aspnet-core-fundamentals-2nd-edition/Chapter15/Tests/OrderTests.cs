using System.Net;
using System.Net.Http.Json;
using AdventureWorks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public sealed class OrderUnitTests
{
    [Fact, Trait("Category", "Unit")]
    public async Task UnavailableInventoryDoesNotPersist()
    {
        var inventory = new Mock<IInventoryService>();
        var repository = new Mock<IOrderRepository>();
        inventory.Setup(i => i.IsAvailableAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = new OrderService(inventory.Object, repository.Object);
        await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
            service.CreateAsync(new(1, 20), CancellationToken.None));
        repository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact, Trait("Category", "Unit")]
    public void DomainAssemblyDoesNotReferencePersistenceOrWeb()
    {
        Assert.DoesNotContain(typeof(Order).Assembly.GetReferencedAssemblies(),
            a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                 a.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
public sealed class OrderApiTests
{
    [Fact, Trait("Category", "Integration")]
    public async Task CreateAndReadPersistedOrder()
    {
        using var factory = new AdventureWorksFactory();
        using var client = factory.CreateInitializedClient();
        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(1, 2));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var order = await client.GetFromJsonAsync<Order>(response.Headers.Location);
        Assert.NotNull(order);
        Assert.Equal(2, order.Quantity);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdventureWorksDbContext>();
        Assert.Equal(1, await db.Orders.AsNoTracking().CountAsync());
    }

    [Theory, InlineData(0, 400), InlineData(20, 409)]
    public async Task InvalidOrUnavailableQuantity(int quantity, int status)
    {
        using var factory = new AdventureWorksFactory();
        using var client = factory.CreateInitializedClient();
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(1, quantity))).StatusCode);
    }

    [Theory, InlineData(null, 401), InlineData("Reader", 403), InlineData("Administrator", 204)]
    public async Task DeleteRequiresAdministrator(string? role, int status)
    {
        using var factory = new AdventureWorksFactory();
        using var client = factory.CreateInitializedClient();
        var created = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(1, 1));
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", "test-user");
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }
        Assert.Equal(status, (int)(await client.DeleteAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(role == "Administrator" ? HttpStatusCode.NotFound : HttpStatusCode.OK,
            (await client.GetAsync(created.Headers.Location)).StatusCode);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task FactoriesDoNotShareRows()
    {
        using var first = new AdventureWorksFactory();
        using var one = first.CreateInitializedClient();
        var created = await one.PostAsJsonAsync("/api/orders", new CreateOrderRequest(1, 1));
        using var second = new AdventureWorksFactory();
        using var two = second.CreateInitializedClient();
        Assert.Equal(HttpStatusCode.NotFound, (await two.GetAsync(created.Headers.Location)).StatusCode);
    }
}
