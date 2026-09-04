using EBOSP.Application.Common;

namespace EBOSP.Infrastructure.Common;

/// <summary>
/// Always-empty ICurrentUserContext for background/system processes with no authenticated HTTP
/// request (e.g. EBOSP.Worker) - every tenant-scoped query such a process runs must therefore use
/// .IgnoreQueryFilters() plus an explicit tenant Where, the same idiom UserRepository's
/// ...IgnoringTenantAsync methods already use before a tenant is known.
/// </summary>
public sealed class NullCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public Guid? TenantId => null;

    public bool HasPermission(string permissionCode) => false;
}
