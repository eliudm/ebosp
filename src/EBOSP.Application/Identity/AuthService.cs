using EBOSP.Application.Common;
using EBOSP.Application.Security;
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
    ISecurityAlertRepository securityAlerts,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    JwtOptions jwtOptions) : IAuthService
{
    // A precomputed hash verified on every login failure path (dummy for a nonexistent user, real
    // otherwise) so response time can't distinguish "no such account" from "wrong password" -
    // without this, skipping the hasher entirely on the fast-fail paths is a timing side channel
    // (PBKDF2 verification costs single-to-double-digit ms; a lookup miss costs microseconds).
    // Static and lazily computed once per process - AuthService is scoped (one instance per
    // request), but the dummy hash itself is fixed and expensive to compute, so it must not be
    // recomputed on every login attempt.
    private static string? _dummyPasswordHash;
    private static readonly Lock DummyPasswordHashLock = new();

    public async Task<TokenResponse> LoginAsync(LoginRequest request, string? clientIp, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailIgnoringTenantAsync(normalizedEmail, cancellationToken);

        var verification = passwordHasher.Verify(user?.PasswordHash ?? GetDummyPasswordHash(), request.Password);

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

        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordFailedLoginWithRetryAsync(user, now, cancellationToken);
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

        if (stored is null)
        {
            throw new AuthenticationFailedException();
        }

        if (stored.RevokedAt is not null)
        {
            // A revoked (already-rotated or explicitly logged-out) token being presented again is
            // a strong signal of theft/reuse, not just an expired session - kill every active
            // session for this user, not just deny this one request (spec §11: "session/token
            // revocation for suspicious or administrative actions").
            await refreshTokens.RevokeAllActiveForUserAsync(stored.UserId, now, cancellationToken);
            events.Record("RefreshTokenReuseDetected", stored.TenantId, nameof(User), stored.UserId.ToString(), new { }, actorId: stored.UserId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (!stored.IsActive(now))
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

    private string GetDummyPasswordHash()
    {
        if (_dummyPasswordHash is not null)
        {
            return _dummyPasswordHash;
        }

        lock (DummyPasswordHashLock)
        {
            return _dummyPasswordHash ??= passwordHasher.Hash("timing-normalization-dummy-password-do-not-use");
        }
    }

    private async Task RecordLoginFailedAsync(User user, string reason, CancellationToken cancellationToken)
    {
        events.Record("LoginFailed", user.TenantId, nameof(User), user.Id.ToString(), new { Reason = reason }, actorId: user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Increments FailedLoginCount with a reload-and-retry loop, guarded by a real EF Core
    /// concurrency token on User (same mechanism as StockBalance/Invoice) - without it, concurrent
    /// failed-login requests against the same account race on a plain in-memory increment and lose
    /// updates, letting an attacker keep the count from ever reaching MaxFailedLoginAttempts and
    /// evading both account lockout and the RepeatedLoginFailures alert below. Mirrors
    /// InventoryService/PaymentService's SaveWithConcurrencyRetryAsync shape.
    /// </summary>
    private async Task RecordFailedLoginWithRetryAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            user.RecordFailedLogin(now);
            events.Record("LoginFailed", user.TenantId, nameof(User), user.Id.ToString(), new { Reason = "invalid credentials" }, actorId: user.Id);

            // The "account locked" branch above short-circuits every later attempt before it ever
            // reaches RecordFailedLogin again, so reaching here with a newly-true IsLockedOut means
            // this exact failure is the one that just tripped it - not a re-raise on every
            // subsequent attempt against an already-locked account (spec §16: "Repeated login
            // failures - 10 failures within configured window - Rate limit + alert").
            if (user.IsLockedOut(now))
            {
                await securityAlerts.AddAsync(
                    SecurityAlert.Raise(
                        user.TenantId,
                        "RepeatedLoginFailures",
                        SecurityAlertSeverity.High,
                        "Account locked after repeated failed login attempts.",
                        now,
                        relatedActorId: user.Id,
                        relatedAggregateType: nameof(User),
                        relatedAggregateId: user.Id.ToString()),
                    cancellationToken);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxAttempts)
            {
                // A failed save leaves the OutboxMessage (and, on the tripping attempt, the
                // SecurityAlert) added-but-uncommitted on this request's scoped DbContext - without
                // discarding them first, the retry would add a second copy of each alongside the
                // first, and both would be inserted together on whichever attempt finally succeeds.
                unitOfWork.DiscardChanges();
                await users.ReloadAsync(user, cancellationToken);
                await Task.Delay(Random.Shared.Next(5, 5 * attempt), cancellationToken);
            }
        }
    }
}
