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
}
