using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Tenant-owned assignment of a <see cref="Role"/> to a <see cref="User"/>, optionally scoped to
/// one <see cref="Branch"/> (spec §12: users' tenant and scope membership).
/// </summary>
public sealed class UserRole : Entity, ITenantOwned
{
    private UserRole()
    {
    }

    public static UserRole Create(Guid tenantId, Guid userId, Guid roleId, Guid? branchId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        UserId = userId,
        RoleId = roleId,
        BranchId = branchId,
    };

    public Guid TenantId { get; private init; }

    public Guid UserId { get; private init; }

    public Guid RoleId { get; private init; }

    public Guid? BranchId { get; private init; }
}
