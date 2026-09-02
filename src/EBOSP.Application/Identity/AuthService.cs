using EBOSP.Application.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

/// <summary>Implements the authentication flow from dev guide §11.1: validate → issue tokens → audit.</summary>
public sealed class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IMfaChallengeProvider mfaChallengeProvider,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    JwtOptions jwtOptions) : IAuthService
{
    public async Task<TokenResponse> LoginAsync(LoginRequest request, string? clientIp, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailIgnoringTenantAsync(normalizedEmail, cancellationToken);

        // Same failure for "no such account" and "wrong password" - spec §11: no account enumeration.
        if (user is null)
        {
            throw new AuthenticationFailedException();
        }

        if (user.Status is UserStatus.Suspended or UserStatus.Disabled)
        {
            await RecordLoginFailedAsync(user, "account not active", cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (user.IsLockedOut(now))
        {
            await RecordLoginFailedAsync(user, "account locked", cancellationToken);
            throw new AuthenticationFailedException();
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin(now);
            await RecordLoginFailedAsync(user, "invalid credentials", cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(request.Password));
        }

        // MFA-ready seam (spec §11: "Add MFA-ready architecture"; §11.1 step 4: "MFA challenge is
        // performed when required"). NoOpMfaChallengeProvider never requires one today - a real
        // provider slots in here without AuthService changing when an org enables MFA.
        if (await mfaChallengeProvider.IsChallengeRequiredAsync(user, cancellationToken))
        {
            throw new MfaChallengeRequiredException();
        }

        user.RecordSuccessfulLogin();

        var response = await IssueTokenPairAsync(user, now, clientIp, cancellationToken);

        events.Record("UserLoggedIn", user.TenantId, nameof(User), user.Id.ToString(), new { user.Email }, actorId: user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, string? clientIp, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var tokenHash = SecureTokenGenerator.Hash(request.RefreshToken);
        var stored = await refreshTokens.FindByHashIgnoringTenantAsync(tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            throw new AuthenticationFailedException();
        }

        var user = await users.GetByIdIgnoringTenantAsync(stored.UserId, cancellationToken);
        if (user is null || !user.CanAuthenticate(now))
        {
            throw new AuthenticationFailedException();
        }

        var response = await IssueTokenPairAsync(user, now, clientIp, cancellationToken, revokedPredecessor: stored);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = SecureTokenGenerator.Hash(request.RefreshToken);
        var stored = await refreshTokens.FindByHashIgnoringTenantAsync(tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive(clock.UtcNow))
        {
            return;
        }

        stored.Revoke(clock.UtcNow);
        events.Record("UserLoggedOut", stored.TenantId, nameof(User), stored.UserId.ToString(), new { }, actorId: stored.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(
        User user,
        DateTimeOffset now,
        string? clientIp,
        CancellationToken cancellationToken,
        RefreshToken? revokedPredecessor = null)
    {
        var permissionCodes = await roles.GetPermissionCodesAsync(user.TenantId, user.Id, cancellationToken);
        var access = jwtTokenService.IssueAccessToken(user.Id, user.TenantId, permissionCodes);

        var refreshTokenValue = SecureTokenGenerator.GenerateToken();
        var refreshToken = RefreshToken.Issue(
            user.TenantId,
            user.Id,
            SecureTokenGenerator.Hash(refreshTokenValue),
            now,
            now.AddDays(jwtOptions.RefreshTokenLifetimeDays),
            clientIp);

        await refreshTokens.AddAsync(refreshToken, cancellationToken);
        revokedPredecessor?.Revoke(now, refreshToken.Id);

        return new TokenResponse(access.Value, refreshTokenValue, access.ExpiresAt);
    }

    private async Task RecordLoginFailedAsync(User user, string reason, CancellationToken cancellationToken)
    {
        events.Record("LoginFailed", user.TenantId, nameof(User), user.Id.ToString(), new { Reason = reason }, actorId: user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
