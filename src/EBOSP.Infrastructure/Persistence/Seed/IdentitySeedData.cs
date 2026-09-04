using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;

namespace EBOSP.Infrastructure.Persistence.Seed;

/// <summary>
/// Deterministic seed rows for the fixed permission/role catalogue (spec §15), applied via EF
/// Core migration HasData so every environment starts with the same reference data.
/// </summary>
internal static class IdentitySeedData
{
    private static readonly Dictionary<string, Guid> PermissionIds = new()
    {
        [PermissionCodes.InventoryRead] = Guid.Parse("11111111-1111-1111-1111-111111111101"),
        [PermissionCodes.InventoryAdjust] = Guid.Parse("11111111-1111-1111-1111-111111111102"),
        [PermissionCodes.ProcurementCreate] = Guid.Parse("11111111-1111-1111-1111-111111111103"),
        [PermissionCodes.ProcurementApprove] = Guid.Parse("11111111-1111-1111-1111-111111111104"),
        [PermissionCodes.PaymentCreate] = Guid.Parse("11111111-1111-1111-1111-111111111105"),
        [PermissionCodes.SecurityAlertManage] = Guid.Parse("11111111-1111-1111-1111-111111111106"),
        [PermissionCodes.AuditRead] = Guid.Parse("11111111-1111-1111-1111-111111111107"),
        [PermissionCodes.UserManage] = Guid.Parse("11111111-1111-1111-1111-111111111108"),
        [PermissionCodes.MasterDataManage] = Guid.Parse("11111111-1111-1111-1111-111111111109"),
        [PermissionCodes.InventoryAdjustLarge] = Guid.Parse("11111111-1111-1111-1111-111111111110"),
        [PermissionCodes.ProcurementApproveLarge] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        [PermissionCodes.SalesCreate] = Guid.Parse("11111111-1111-1111-1111-111111111112"),
        [PermissionCodes.SalesFulfill] = Guid.Parse("11111111-1111-1111-1111-111111111113"),
        [PermissionCodes.InvoiceCreate] = Guid.Parse("11111111-1111-1111-1111-111111111114"),
        [PermissionCodes.PaymentCreateLarge] = Guid.Parse("11111111-1111-1111-1111-111111111115"),
    };

    private static readonly Dictionary<string, Guid> RoleIds = new()
    {
        [RoleCodes.TenantAdmin] = Guid.Parse("22222222-2222-2222-2222-222222222201"),
        [RoleCodes.WarehouseManager] = Guid.Parse("22222222-2222-2222-2222-222222222202"),
        [RoleCodes.Procurement] = Guid.Parse("22222222-2222-2222-2222-222222222203"),
        [RoleCodes.Manager] = Guid.Parse("22222222-2222-2222-2222-222222222204"),
        [RoleCodes.Finance] = Guid.Parse("22222222-2222-2222-2222-222222222205"),
        [RoleCodes.SecurityOfficer] = Guid.Parse("22222222-2222-2222-2222-222222222206"),
        [RoleCodes.Auditor] = Guid.Parse("22222222-2222-2222-2222-222222222207"),
        [RoleCodes.SalesOfficer] = Guid.Parse("22222222-2222-2222-2222-222222222208"),
    };

    public static readonly Permission[] Permissions =
    [
        Permission.Create(PermissionIds[PermissionCodes.InventoryRead], PermissionCodes.InventoryRead, "Read inventory balances and ledger."),
        Permission.Create(PermissionIds[PermissionCodes.InventoryAdjust], PermissionCodes.InventoryAdjust, "Adjust stock balances."),
        Permission.Create(PermissionIds[PermissionCodes.ProcurementCreate], PermissionCodes.ProcurementCreate, "Create purchase requests."),
        Permission.Create(PermissionIds[PermissionCodes.ProcurementApprove], PermissionCodes.ProcurementApprove, "Approve purchase requests."),
        Permission.Create(PermissionIds[PermissionCodes.PaymentCreate], PermissionCodes.PaymentCreate, "Record payments."),
        Permission.Create(PermissionIds[PermissionCodes.SecurityAlertManage], PermissionCodes.SecurityAlertManage, "Manage security alerts."),
        Permission.Create(PermissionIds[PermissionCodes.AuditRead], PermissionCodes.AuditRead, "Read audit events."),
        Permission.Create(PermissionIds[PermissionCodes.UserManage], PermissionCodes.UserManage, "Manage users, roles and tenant administration."),
        Permission.Create(PermissionIds[PermissionCodes.MasterDataManage], PermissionCodes.MasterDataManage, "Manage branches, warehouses, product categories and products."),
        Permission.Create(PermissionIds[PermissionCodes.InventoryAdjustLarge], PermissionCodes.InventoryAdjustLarge, "Perform stock adjustments above the large-adjustment threshold."),
        Permission.Create(PermissionIds[PermissionCodes.ProcurementApproveLarge], PermissionCodes.ProcurementApproveLarge, "Approve purchase requests above the high-value threshold."),
        Permission.Create(PermissionIds[PermissionCodes.SalesCreate], PermissionCodes.SalesCreate, "Manage customers, quotations and sales orders."),
        Permission.Create(PermissionIds[PermissionCodes.SalesFulfill], PermissionCodes.SalesFulfill, "Create deliveries against a sales order."),
        Permission.Create(PermissionIds[PermissionCodes.InvoiceCreate], PermissionCodes.InvoiceCreate, "Create invoices for fulfilled sales orders."),
        Permission.Create(PermissionIds[PermissionCodes.PaymentCreateLarge], PermissionCodes.PaymentCreateLarge, "Confirm payments above the large-payment threshold."),
    ];

    public static readonly Role[] Roles =
    [
        Role.Create(RoleIds[RoleCodes.TenantAdmin], RoleCodes.TenantAdmin, "Tenant Admin"),
        Role.Create(RoleIds[RoleCodes.WarehouseManager], RoleCodes.WarehouseManager, "Warehouse Manager"),
        Role.Create(RoleIds[RoleCodes.Procurement], RoleCodes.Procurement, "Procurement"),
        Role.Create(RoleIds[RoleCodes.Manager], RoleCodes.Manager, "Manager"),
        Role.Create(RoleIds[RoleCodes.Finance], RoleCodes.Finance, "Finance"),
        Role.Create(RoleIds[RoleCodes.SecurityOfficer], RoleCodes.SecurityOfficer, "Security Officer"),
        Role.Create(RoleIds[RoleCodes.Auditor], RoleCodes.Auditor, "Auditor"),
        Role.Create(RoleIds[RoleCodes.SalesOfficer], RoleCodes.SalesOfficer, "Sales Officer"),
    ];

    public static readonly RolePermission[] RolePermissions =
    [
        .. PermissionCodes.All.Select(code => RolePermission.Create(RoleIds[RoleCodes.TenantAdmin], PermissionIds[code])),
        RolePermission.Create(RoleIds[RoleCodes.WarehouseManager], PermissionIds[PermissionCodes.InventoryRead]),
        RolePermission.Create(RoleIds[RoleCodes.WarehouseManager], PermissionIds[PermissionCodes.InventoryAdjust]),
        RolePermission.Create(RoleIds[RoleCodes.Procurement], PermissionIds[PermissionCodes.ProcurementCreate]),
        RolePermission.Create(RoleIds[RoleCodes.Manager], PermissionIds[PermissionCodes.ProcurementApprove]),
        RolePermission.Create(RoleIds[RoleCodes.Finance], PermissionIds[PermissionCodes.PaymentCreate]),
        RolePermission.Create(RoleIds[RoleCodes.Finance], PermissionIds[PermissionCodes.InvoiceCreate]),
        // payment.create.large is deliberately NOT granted to finance - there's no second, senior
        // finance role in this catalogue to split it against, so large payments need tenant-admin
        // (granted automatically below), the same relationship a teller/supervisor limit models.
        // Finance needs the base procurement.approve permission too, not just the ".large" tier -
        // PurchaseRequestsController.Approve's [Authorize(Policy=procurement.approve)] gates the
        // action unconditionally, and the ".large" check only runs after that already succeeded
        // (same relationship as inventory.adjust/inventory.adjust.large in M4).
        RolePermission.Create(RoleIds[RoleCodes.Finance], PermissionIds[PermissionCodes.ProcurementApprove]),
        RolePermission.Create(RoleIds[RoleCodes.Finance], PermissionIds[PermissionCodes.ProcurementApproveLarge]),
        RolePermission.Create(RoleIds[RoleCodes.SecurityOfficer], PermissionIds[PermissionCodes.SecurityAlertManage]),
        RolePermission.Create(RoleIds[RoleCodes.Auditor], PermissionIds[PermissionCodes.AuditRead]),
        RolePermission.Create(RoleIds[RoleCodes.SalesOfficer], PermissionIds[PermissionCodes.SalesCreate]),
        RolePermission.Create(RoleIds[RoleCodes.SalesOfficer], PermissionIds[PermissionCodes.SalesFulfill]),
    ];
}
