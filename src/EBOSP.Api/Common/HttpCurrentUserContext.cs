using System.Security.Claims;
using EBOSP.Application.Authorization;
using EBOSP.Application.Common;

namespace EBOSP.Api.Common;

/// <summary>
/// Reads the current user/tenant from the authenticated HTTP request principal. Until Identity
/// (Phase 2) issues claims, there is no authenticated principal, so every property is empty.
/// </summary>
public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => TryGetClaimGuid(ClaimTypes.NameIdentifier);

    public Guid? TenantId => TryGetClaimGuid("tenant_id");

    public bool HasPermission(string permissionCode) => Principal?.HasClaim(PermissionClaimTypes.Permission, permissionCode) ?? false;

    private Guid? TryGetClaimGuid(string claimType)
    {
        var value = Principal?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
