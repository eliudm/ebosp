namespace EBOSP.Domain.Identity;

/// <summary>Join row for the fixed, seeded role-to-permission catalogue (spec §15).</summary>
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public static RolePermission Create(Guid roleId, Guid permissionId) => new()
    {
        RoleId = roleId,
        PermissionId = permissionId,
    };

    public Guid RoleId { get; private init; }

    public Guid PermissionId { get; private init; }
}
