using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    /// <summary>Looks up by hash without applying the tenant query filter - the caller's tenant is derived from the token itself.</summary>
    Task<RefreshToken?> FindByHashIgnoringTenantAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Revokes every currently-active session for a user - used on password reset, matching spec §11's "session/token revocation for suspicious or administrative actions".</summary>
    Task RevokeAllActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
