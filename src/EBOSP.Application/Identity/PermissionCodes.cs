namespace EBOSP.Application.Identity;

/// <summary>Fixed platform permission catalogue (spec §15) - not tenant-customizable.</summary>
public static class PermissionCodes
{
    public const string InventoryRead = "inventory.read";
    public const string InventoryAdjust = "inventory.adjust";
    public const string InventoryAdjustLarge = "inventory.adjust.large";
    public const string ProcurementCreate = "procurement.create";
    public const string ProcurementApprove = "procurement.approve";
    public const string ProcurementApproveLarge = "procurement.approve.large";
    public const string SalesCreate = "sales.create";
    public const string SalesFulfill = "sales.fulfill";
    public const string InvoiceCreate = "invoice.create";
    public const string PaymentCreate = "payment.create";
    public const string PaymentCreateLarge = "payment.create.large";
    public const string SecurityAlertManage = "security.alert.manage";
    public const string AuditRead = "audit.read";
    public const string UserManage = "user.manage";
    public const string MasterDataManage = "master-data.manage";

    public static readonly IReadOnlyList<string> All =
    [
        InventoryRead,
        InventoryAdjust,
        InventoryAdjustLarge,
        ProcurementCreate,
        ProcurementApprove,
        ProcurementApproveLarge,
        SalesCreate,
        SalesFulfill,
        InvoiceCreate,
        PaymentCreate,
        PaymentCreateLarge,
        SecurityAlertManage,
        AuditRead,
        UserManage,
        MasterDataManage,
    ];
}
