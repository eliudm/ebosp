using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Header record for receiving stock (dev guide §13.1). <see cref="PurchaseOrderId"/> is nullable -
/// spec §8.2 allows receiving "against a purchase order or approved receipt", and Inventory is
/// built before Procurement, so this must work standalone today.
/// </summary>
public sealed class GoodsReceipt : Entity, ITenantOwned
{
    private readonly List<GoodsReceiptLine> _lines = [];

    private GoodsReceipt()
    {
    }

    public static GoodsReceipt Create(
        Guid tenantId,
        Guid warehouseId,
        Guid receivedByUserId,
        DateTimeOffset receivedAt,
        Guid? purchaseOrderId,
        string? reference,
        string? idempotencyKey,
        IReadOnlyCollection<(Guid ProductId, int Quantity)> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A goods receipt must have at least one line.", nameof(lines));
        }

        var receipt = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ReceivedByUserId = receivedByUserId,
            ReceivedAt = receivedAt,
            PurchaseOrderId = purchaseOrderId,
            Reference = reference,
            IdempotencyKey = idempotencyKey,
        };

        foreach (var (productId, quantity) in lines)
        {
            receipt._lines.Add(GoodsReceiptLine.Create(tenantId, receipt.Id, productId, quantity));
        }

        return receipt;
    }

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ReceivedByUserId { get; private init; }

    public DateTimeOffset ReceivedAt { get; private init; }

    public Guid? PurchaseOrderId { get; private init; }

    public string? Reference { get; private init; }

    public string? IdempotencyKey { get; private init; }

    public IReadOnlyList<GoodsReceiptLine> Lines => _lines;
}
