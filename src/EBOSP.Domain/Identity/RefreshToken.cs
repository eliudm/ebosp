using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// A revocable, rotating refresh session (spec §11/§14: "secure refresh/session handling",
/// "session/token revocation for suspicious or administrative actions"). The access token (JWT)
/// itself carries no revocation state - this row is what makes revocation possible.
/// </summary>
public sealed class RefreshToken : Entity, ITenantOwned
{
    private RefreshToken()
    {
    }

    public static RefreshToken Issue(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        string? createdByIp) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
        };

    public Guid TenantId { get; private init; }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private init; } = null!;

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public string? CreatedByIp { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, Guid? replacedByTokenId = null)
    {
        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
