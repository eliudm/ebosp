using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Proof of shipment for one SalesOrder (dev guide §15: "Delivery is a separate auditable state
/// transition"). One delivery per order, full-order only - no partial shipments this phase.
/// </summary>
public sealed class Delivery : Entity, ITenantOwned
{
    private readonly List<DeliveryLine> _lines = [];

    private Delivery()
    {
    }

    public static Delivery Create(
        Guid tenantId,
        Guid salesOrderId,
        Guid warehouseId,
        Guid deliveredByUserId,
        DateTimeOffset deliveredAt,
        IReadOnlyCollection<(Guid ProductId, int Quantity)> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A delivery must have at least one line.", nameof(lines));
        }

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            WarehouseId = warehouseId,
            DeliveredByUserId = deliveredByUserId,
            DeliveredAt = deliveredAt,
        };

        foreach (var (productId, quantity) in lines)
        {
            delivery._lines.Add(DeliveryLine.Create(tenantId, delivery.Id, productId, quantity));
        }

        return delivery;
    }

    public Guid TenantId { get; private init; }

    public Guid SalesOrderId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid DeliveredByUserId { get; private init; }

    public DateTimeOffset DeliveredAt { get; private init; }

    public IReadOnlyList<DeliveryLine> Lines => _lines;
}
