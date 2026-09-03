using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Current stock projection for one (warehouse, product) pair (dev guide §13.2: "Current balance
/// may be stored as a projection for performance, but the ledger remains the audit history").
/// Concurrency is enforced at the EF Core mapping level via Postgres's xmin system column, not a
/// domain-visible property - see AppDbContext.
/// </summary>
public sealed class StockBalance : Entity, ITenantOwned
{
    private StockBalance()
    {
    }

    public static StockBalance CreateEmpty(Guid tenantId, Guid warehouseId, Guid productId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        WarehouseId = warehouseId,
        ProductId = productId,
        QuantityOnHand = 0,
        QuantityReserved = 0,
    };

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public int QuantityOnHand { get; private set; }

    public int QuantityReserved { get; private set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    public void Receive(int quantity)
    {
        RequirePositive(quantity);
        QuantityOnHand += quantity;
    }

    /// <summary>Negative stock is hard-blocked (spec §8.2/dev guide §13.2's "explicit/configurable policy" - this is the policy).</summary>
    public void Issue(int quantity)
    {
        RequirePositive(quantity);
        if (quantity > QuantityAvailable)
        {
            throw new InvalidOperationException("Insufficient available stock.");
        }

        QuantityOnHand -= quantity;
    }

    public void Reserve(int quantity)
    {
        RequirePositive(quantity);
        if (quantity > QuantityAvailable)
        {
            throw new InvalidOperationException("Insufficient available stock.");
        }

        QuantityReserved += quantity;
    }

    public void Release(int quantity)
    {
        RequirePositive(quantity);
        if (quantity > QuantityReserved)
        {
            throw new InvalidOperationException("Cannot release more than is reserved.");
        }

        QuantityReserved -= quantity;
    }

    public void AdjustBy(int delta)
    {
        if (delta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), "Adjustment delta cannot be zero.");
        }

        if (QuantityOnHand + delta < 0)
        {
            throw new InvalidOperationException("Adjustment would result in negative stock.");
        }

        QuantityOnHand += delta;
    }

    private static void RequirePositive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
    }
}
