using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum BranchStatus
{
    Active,
    Inactive,
}

/// <summary>Operational location (dev guide §12) - other master data (Warehouse, Product) scopes off it.</summary>
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

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Activate() => Status = BranchStatus.Active;

    public void Deactivate() => Status = BranchStatus.Inactive;
}
