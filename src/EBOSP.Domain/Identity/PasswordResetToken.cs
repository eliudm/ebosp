using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// A single-use, short-lived password reset token (spec §11: "password reset/recovery without
/// revealing whether an account exists"). Only the hash is ever persisted, mirroring
/// <see cref="RefreshToken"/>.
/// </summary>
public sealed class PasswordResetToken : Entity, ITenantOwned
{
    private PasswordResetToken()
    {
    }

    public static PasswordResetToken Issue(Guid tenantId, Guid userId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = expiresAt,
    };

    public Guid TenantId { get; private init; }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private init; } = null!;

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
}
