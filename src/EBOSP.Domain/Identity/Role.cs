using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// A fixed, platform-defined role (spec §15) - not tenant-customizable. Rows are seeded via
/// migration from <see cref="EBOSP.Application.Identity.RoleCodes"/>.
/// </summary>
public sealed class Role : Entity
{
    private Role()
    {
    }

    public static Role Create(Guid id, string code, string name) => new()
    {
        Id = id,
        Code = code,
        Name = name,
    };

    public string Code { get; private init; } = null!;

    public string Name { get; private init; } = null!;
}
