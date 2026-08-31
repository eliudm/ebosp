using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// A fixed, platform-defined permission (spec §15) - not tenant-customizable. Rows are seeded via
/// migration from <see cref="EBOSP.Application.Identity.PermissionCodes"/>.
/// </summary>
public sealed class Permission : Entity
{
    private Permission()
    {
    }

    public static Permission Create(Guid id, string code, string description) => new()
    {
        Id = id,
        Code = code,
        Description = description,
    };

    public string Code { get; private init; } = null!;

    public string Description { get; private init; } = null!;
}
