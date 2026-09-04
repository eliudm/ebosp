using EBOSP.Application.Identity;

namespace EBOSP.Application.Documents;

/// <summary>
/// The small, explicit set of parent entities documents can be attached to this phase (spec §18:
/// "Authorize every download using the parent business entity's permissions") - not a blanket
/// document.manage permission, and not fully generic reflection-based entity authorization. Adding
/// a third supported entity type later is one line here, not an architecture change.
/// </summary>
public static class DocumentEntityTypes
{
    public const string PurchaseOrder = "PurchaseOrder";
    public const string Invoice = "Invoice";

    public static readonly IReadOnlyDictionary<string, string> WritePermissionByEntityType = new Dictionary<string, string>
    {
        [PurchaseOrder] = PermissionCodes.ProcurementCreate,
        [Invoice] = PermissionCodes.InvoiceCreate,
    };
}
