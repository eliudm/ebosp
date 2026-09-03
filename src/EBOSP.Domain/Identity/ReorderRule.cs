using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Per-warehouse override of <see cref="Product.ReorderLevel"/> (dev guide §13.1 lists this as its
/// own entity; dev guide §12 groups "reorder rules" under master data configuration, not
/// inventory operations - matches this entity's permission gating at the controller).
/// </summary>
public sealed class ReorderRule : Entity, ITenantOwned
{
    private ReorderRule()
    {
    }

    public static ReorderRule Create(Guid tenantId, Guid warehouseId, Guid productId, int reorderLevel)
    {
        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel), "Reorder level cannot be negative.");
        }

        return new ReorderRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductId = productId,
            ReorderLevel = reorderLevel,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public int ReorderLevel { get; private set; }

    public void Update(int reorderLevel)
    {
        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel), "Reorder level cannot be negative.");
        }

        ReorderLevel = reorderLevel;
    }
}
