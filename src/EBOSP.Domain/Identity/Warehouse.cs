using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum WarehouseStatus
{
    Active,
    Inactive,
}

/// <summary>Inventory location within a branch (dev guide §12, spec §7: WarehouseId, BranchId).</summary>
public sealed class Warehouse : Entity, ITenantOwned
{
    private Warehouse()
    {
    }

    public static Warehouse Create(Guid tenantId, Guid branchId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Warehouse name is required.", nameof(name));
        }

        return new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Name = name.Trim(),
            Status = WarehouseStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid BranchId { get; private init; }

    public string Name { get; private set; } = null!;

    public WarehouseStatus Status { get; private set; }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Warehouse name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Activate() => Status = WarehouseStatus.Active;

    public void Deactivate() => Status = WarehouseStatus.Inactive;
}
