using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using WarehouseApi.ErrorHandling;
using WarehouseApi.Exceptions;
using WarehouseApi.Models;

namespace WarehouseApi.Tests;

// The translation of an EF Core conflict, tested without a database: For receives the entity that EF Core
// reports in DbUpdateConcurrencyException.Entries
public class ConcurrencyTranslationTests
{
    [Fact]
    public void AnOrder_IsDescribedByItsId()
    {
        var order = new Order { Id = Guid.Parse("6a0f8e1c-3b2d-4c5e-9f7a-1b2c3d4e5f60") };

        var exception = ConcurrentModificationException.For(order);

        Assert.Equal("Order", exception.ResourceType);
        Assert.Equal("6a0f8e1c-3b2d-4c5e-9f7a-1b2c3d4e5f60", exception.ResourceId);
    }

    [Fact]
    public void AStockItem_IsDescribedByItsWarehouseAndSku_NotByItsKey()
    {
        var item = new StockItem { Id = Guid.NewGuid(), WarehouseCode = "WH-001", ProductSku = "SKU-001" };

        var exception = ConcurrentModificationException.For(item);

        Assert.Equal("StockItem", exception.ResourceType);
        Assert.Equal("WH-001/SKU-001", exception.ResourceId);
    }

    [Fact]
    public void TheMapper_TurnsDbUpdateConcurrencyExceptionInto409()
    {
        var mapper = new ExceptionToProblemDetailsMapper(new HostingEnvironment { EnvironmentName = Environments.Production });

        // Without entries, as EF Core reports a conflict that names no entity
        var problem = mapper.Map(new DbUpdateConcurrencyException("The row was changed."), new DefaultHttpContext());

        Assert.Equal(409, problem.Status);
        Assert.Equal("CONCURRENT_MODIFICATION", problem.Extensions["conflictReason"]);
        Assert.Equal("Resource", problem.Extensions["resourceType"]);
        Assert.False(problem.Extensions.ContainsKey("expectedVersion"));
    }
}
