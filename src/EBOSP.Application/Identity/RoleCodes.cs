namespace EBOSP.Application.Identity;

/// <summary>Fixed platform role catalogue seeded from spec §15's sample authorization table.</summary>
public static class RoleCodes
{
    public const string TenantAdmin = "tenant-admin";
    public const string WarehouseManager = "warehouse-manager";
    public const string Procurement = "procurement";
    public const string Manager = "manager";
    public const string Finance = "finance";
    public const string SalesOfficer = "sales-officer";
    public const string SecurityOfficer = "security-officer";
    public const string Auditor = "auditor";
}
