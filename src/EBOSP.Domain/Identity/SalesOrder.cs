using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum SalesOrderStatus
{
    Open,
    Cancelled,
    Fulfilled,
}

/// <summary>
/// Sales order (ERD: OrderId, CustomerId, Status, Total; dev guide §15). Always converted from an
/// Accepted Quotation (mirrors PurchaseOrder.PurchaseRequestId being mandatory) - lines are copied
/// verbatim from the quotation the customer accepted, not re-entered, since the quotation is a
/// price commitment made to them.
/// </summary>
public sealed class SalesOrder : Entity, ITenantOwned
{
    private readonly List<SalesOrderLine> _lines = [];

    private SalesOrder()
    {
    }

    public static SalesOrder Create(
        Guid tenantId,
        Guid quotationId,
        Guid customerId,
        Guid branchId,
        Guid warehouseId,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        IReadOnlyCollection<(Guid ProductId, int Quantity, decimal UnitPrice, Guid ReservationId)> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A sales order must have at least one line.", nameof(lines));
        }

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            QuotationId = quotationId,
            CustomerId = customerId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            Status = SalesOrderStatus.Open,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };

        foreach (var (productId, quantity, unitPrice, reservationId) in lines)
        {
            order._lines.Add(SalesOrderLine.Create(tenantId, order.Id, productId, quantity, unitPrice, reservationId));
        }

        order.Total = order._lines.Sum(l => l.LineTotal);

        return order;
    }

    public Guid TenantId { get; private init; }

    public Guid QuotationId { get; private init; }

    public Guid CustomerId { get; private init; }

    public Guid BranchId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public SalesOrderStatus Status { get; private set; }

    public decimal Total { get; private set; }

    public Guid CreatedByUserId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<SalesOrderLine> Lines => _lines;

    public void MarkFulfilled()
    {
        if (Status != SalesOrderStatus.Open)
        {
            throw new InvalidOperationException("Only an open sales order can be fulfilled.");
        }

        Status = SalesOrderStatus.Fulfilled;
    }
}
