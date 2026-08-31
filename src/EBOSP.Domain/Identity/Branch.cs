using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum BranchStatus
{
    Active,
    Inactive,
}

/// <summary>
/// Minimal branch shell for role/user scoping in Phase 2. Warehouses, products and the rest of
/// master data (dev guide §12) are built out in Phase 3 - this is not that module.
/// </summary>
public sealed class Branch : Entity, ITenantOwned
{
    private Branch()
    {
    }

    public static Branch Create(Guid tenantId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        return new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Status = BranchStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public string Name { get; private set; } = null!;

    public BranchStatus Status { get; private set; }
}
