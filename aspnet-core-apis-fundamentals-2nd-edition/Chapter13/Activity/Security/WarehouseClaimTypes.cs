namespace WarehouseApi.Security;

public static class WarehouseClaimTypes
{
    // The warehouse a staff member or an integration client works with, for example WH-001
    public const string WarehouseId = "WarehouseId";

    // An inventory auditor's clearance level, a whole number such as 2
    public const string AuditClearance = "AuditClearance";
}
