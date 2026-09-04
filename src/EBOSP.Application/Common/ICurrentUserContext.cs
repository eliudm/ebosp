namespace EBOSP.Application.Common;

/// <summary>
/// Abstraction over "who is making this request" so Application/Infrastructure code never reads
/// HttpContext directly (dev guide §9, foundation module). Populated from the authenticated
/// principal once Identity (Phase 2) exists; until then, implementations may return an
/// unauthenticated context.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? TenantId { get; }

    /// <summary>
    /// Whether the current principal holds the given permission claim (the same claim
    /// <c>PermissionAuthorizationHandler</c> checks for policy-based authorization) - needed by
    /// code that must enforce a permission without an ASP.NET Core <c>[Authorize(Policy=...)]</c>
    /// to lean on, e.g. the AI assistant choosing which tools to even offer the model (dev guide
    /// §43: "AI receives only data the requesting user is already permitted to see").
    /// </summary>
    bool HasPermission(string permissionCode);
}
