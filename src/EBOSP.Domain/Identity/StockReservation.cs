using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum StockReservationStatus
{
    Active,
    Released,
}

/// <summary>
/// Earmarks stock against <see cref="StockBalance.QuantityReserved"/> (dev guide §13.1, spec §7's
/// StockBalance.Reserved field). No auto-expiry - undocumented in both governing docs; explicit
/// release only until Sales (M6) defines real reservation lifecycle needs.
/// </summary>
public sealed class StockReservation : Entity, ITenantOwned
{
    private StockReservation()
    {
    }

    public static StockReservation Create(Guid tenantId, Guid warehouseId, Guid productId, int quantity, DateTimeOffset createdAt)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        return new StockReservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            Status = StockReservationStatus.Active,
            CreatedAt = createdAt,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }

    public StockReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public void Release(DateTimeOffset releasedAt)
    {
        if (Status == StockReservationStatus.Released)
        {
            throw new InvalidOperationException("Reservation is already released.");
        }

        Status = StockReservationStatus.Released;
        ReleasedAt = releasedAt;
    }
}
