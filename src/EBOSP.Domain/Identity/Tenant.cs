using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum TenantStatus
{
    Active,
    Suspended,
}

public sealed class Tenant : Entity
{
    private Tenant()
    {
    }

    public static Tenant Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name is required.", nameof(name));
        }

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Status = TenantStatus.Active,
        };
    }

    public string Name { get; private set; } = null!;

    public TenantStatus Status { get; private set; }

    public void Suspend() => Status = TenantStatus.Suspended;
}
