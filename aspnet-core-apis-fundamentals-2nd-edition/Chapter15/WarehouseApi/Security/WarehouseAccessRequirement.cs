using Microsoft.AspNetCore.Authorization;

namespace WarehouseApi.Security;

public class WarehouseAccessRequirement : IAuthorizationRequirement
{
    public string RequiredPermission { get; }

    public WarehouseAccessRequirement(string requiredPermission)
    {
        RequiredPermission = requiredPermission;
    }
}
