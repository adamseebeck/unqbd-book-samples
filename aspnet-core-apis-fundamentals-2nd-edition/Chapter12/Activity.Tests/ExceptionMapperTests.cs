using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using WarehouseApi.ErrorHandling;
using WarehouseApi.Exceptions;

namespace WarehouseApi.Tests;

// The mapper is a plain class, so it can be tested without a web host
public class ExceptionMapperTests
{
    private readonly ExceptionToProblemDetailsMapper _mapper =
        new(new HostingEnvironment { EnvironmentName = Environments.Production });

    [Fact]
    public void InsufficientStock_MapsToConflictWithTheStockDetails()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/Orders/1/allocate";

        var problem = _mapper.Map(new InsufficientStockException("SKU-001", "WH-001", requested: 500, available: 130), context);

        Assert.Equal(409, problem.Status);
        Assert.Equal("INSUFFICIENT_STOCK", problem.Extensions["conflictReason"]);
        Assert.Equal("SKU-001", problem.Extensions["sku"]);
        Assert.Equal(130, problem.Extensions["availableQuantity"]);
        Assert.Equal("/api/Orders/1/allocate", problem.Instance);
    }

    [Fact]
    public void UnexpectedException_KeepsItsMessageOutOfTheResponse()
    {
        var problem = _mapper.Map(new TimeoutException("Server=sql-prod-01 did not answer"), new DefaultHttpContext());

        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("sql-prod-01", problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("stackTrace"));
    }
}
