using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum PurchaseOrderStatus
{
    Open,
    Cancelled,
    Fulfilled,
}

/// <summary>
/// Purchase order (ERD: POId, SupplierId, Status, Total; dev guide §14). Line items are entered
/// fresh at PO-creation time rather than strictly derived from the linked PurchaseRequest's
/// estimated lines - real procurement negotiates final supplier pricing, and neither governing doc
/// requires a strict line-by-line match. GoodsReceipt.PurchaseOrderId (built in M4) is the only
/// forward link from receiving back to this entity - fulfillment/cancellation transitions are out
/// of this phase's scope (no module today reads PurchaseOrder.Status past creation).
/// </summary>
public sealed class PurchaseOrder : Entity, ITenantOwned
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    public static PurchaseOrder Create(
        Guid tenantId,
        Guid purchaseRequestId,
        Guid supplierId,
        string poNumber,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        IReadOnlyCollection<(Guid ProductId, int Quantity, decimal UnitPrice)> lines)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            throw new ArgumentException("A PO number is required.", nameof(poNumber));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("A purchase order must have at least one line.", nameof(lines));
        }

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseRequestId = purchaseRequestId,
            SupplierId = supplierId,
            PoNumber = poNumber.Trim(),
            Status = PurchaseOrderStatus.Open,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };

        foreach (var (productId, quantity, unitPrice) in lines)
        {
            order._lines.Add(PurchaseOrderLine.Create(tenantId, order.Id, productId, quantity, unitPrice));
        }

        order.Total = order._lines.Sum(l => l.LineTotal);

        return order;
    }

    public Guid TenantId { get; private init; }

    public Guid PurchaseRequestId { get; private init; }

    public Guid SupplierId { get; private init; }

    public string PoNumber { get; private init; } = null!;

    public PurchaseOrderStatus Status { get; private init; }

    public decimal Total { get; private set; }

    public Guid CreatedByUserId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;
}
