namespace WarehouseApi.Security;

public static class AuthorizationPolicies
{
    public const string RequireWarehouseAccess = "RequireWarehouseAccess";
    public const string RequireSupervisor = "RequireSupervisor";
    public const string CanAdjustStock = "CanAdjustStock";
    public const string RequireIntegrationClientOrSupervisor = "RequireIntegrationClientOrSupervisor";
    public const string CanViewAuditHistory = "CanViewAuditHistory";
}
