using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Carries the StockReservation created for this line at order-creation time (dev guide §15:
/// "reserve inventory... enforce the rule transactionally") so Delivery can release the exact
/// reservation when the order ships.
/// </summary>
public sealed class SalesOrderLine : Entity, ITenantOwned
{
    private SalesOrderLine()
    {
    }

    internal static SalesOrderLine Create(Guid tenantId, Guid salesOrderId, Guid productId, int quantity, decimal unitPrice, Guid reservationId)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        return new SalesOrderLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            ReservationId = reservationId,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid SalesOrderId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }

    public decimal UnitPrice { get; private init; }

    public Guid ReservationId { get; private init; }

    public decimal LineTotal => Quantity * UnitPrice;
}
