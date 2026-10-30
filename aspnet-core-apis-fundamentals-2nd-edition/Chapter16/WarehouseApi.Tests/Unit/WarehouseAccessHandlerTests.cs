using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WarehouseApi.Security;

namespace WarehouseApi.Tests.Unit;

// The handler is plain code: build a user and a requirement, call it, and read the result
public class WarehouseAccessHandlerTests
{
    private readonly WarehouseAccessHandler _handler = new();

    [Theory]
    [InlineData(UserRoles.WarehouseStaff, "WH-001", Permissions.Adjust, "WH-001", true)]
    [InlineData(UserRoles.WarehouseStaff, "WH-001", Permissions.Adjust, "WH-002", false)]
    [InlineData(UserRoles.WarehouseStaff, "WH-001", Permissions.Read, "wh-001", true)]
    [InlineData(UserRoles.IntegrationClient, "WH-002", Permissions.Read, "WH-002", true)]
    [InlineData(UserRoles.IntegrationClient, "WH-002", Permissions.Adjust, "WH-002", false)]
    [InlineData(UserRoles.Supervisor, null, Permissions.Fulfill, "WH-002", true)]
    public async Task Decides_ByRoleWarehouseAndPermission(
        string role, string? userWarehouse, string permission, string warehouseCode, bool expected)
    {
        var requirement = new WarehouseAccessRequirement(permission);
        var context = new AuthorizationHandlerContext([requirement], CreateUser(role, userWarehouse), warehouseCode);

        await _handler.HandleAsync(context);

        Assert.Equal(expected, context.HasSucceeded);
    }

    [Fact]
    public async Task DoesNotSucceed_WithoutAWarehouseToCheck()
    {
        var requirement = new WarehouseAccessRequirement(Permissions.Read);
        var context = new AuthorizationHandlerContext([requirement], CreateUser(UserRoles.WarehouseStaff, "WH-001"), resource: null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreateUser(string role, string? warehouseCode)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (warehouseCode is not null)
        {
            claims.Add(new Claim(WarehouseClaimTypes.WarehouseId, warehouseCode));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
