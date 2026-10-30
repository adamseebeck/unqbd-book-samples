using Microsoft.AspNetCore.Authorization;

namespace WarehouseApi.Security;

public class AuditAccessRequirement : IAuthorizationRequirement
{
    public int MinimumClearanceLevel { get; }

    public AuditAccessRequirement(int minimumClearanceLevel)
    {
        MinimumClearanceLevel = minimumClearanceLevel;
    }
}
