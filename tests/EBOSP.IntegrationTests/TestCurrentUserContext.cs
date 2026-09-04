using EBOSP.Application.Common;

namespace EBOSP.IntegrationTests;

public sealed class TestCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => TenantId is not null;

    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public HashSet<string> Permissions { get; } = [];

    public bool HasPermission(string permissionCode) => Permissions.Contains(permissionCode);
}
