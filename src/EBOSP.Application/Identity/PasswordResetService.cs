using EBOSP.Application.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

/// <summary>Password reset/recovery without revealing whether an account exists (spec §11).</summary>
public sealed class PasswordResetService(
    IUserRepository users,
    IPasswordResetTokenRepository resetTokens,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IPasswordResetNotifier notifier,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IPasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);

    public async Task RequestResetAsync(PasswordResetRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailIgnoringTenantAsync(normalizedEmail, cancellationToken);

        // No branch here reveals to the caller whether the account exists or is active - this
        // method always returns successfully either way (spec §11: no account enumeration).
        if (user is null || user.Status != UserStatus.Active)
        {
            return;
        }

        var tokenValue = SecureTokenGenerator.GenerateToken();
        var resetToken = PasswordResetToken.Issue(user.TenantId, user.Id, SecureTokenGenerator.Hash(tokenValue), now, now.Add(TokenLifetime));
        await resetTokens.AddAsync(resetToken, cancellationToken);

        events.Record("PasswordResetRequested", user.TenantId, nameof(User), user.Id.ToString(), new { }, actorId: user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Delivered outside the transaction: a notification failure must not roll back the
        // issued token, and the caller gets the same response either way.
        await notifier.NotifyAsync(user.Email, tokenValue, cancellationToken);
    }

    public async Task ConfirmResetAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var tokenHash = SecureTokenGenerator.Hash(request.Token);
        var stored = await resetTokens.FindByHashIgnoringTenantAsync(tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            throw new AuthenticationFailedException();
        }

        var user = await users.GetByIdIgnoringTenantAsync(stored.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            throw new AuthenticationFailedException();
        }

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        stored.MarkUsed(now);

        // A password reset is a strong signal that any existing session may be compromised -
        // revoke every active refresh token so a stolen session can't outlive the reset.
        await refreshTokens.RevokeAllActiveForUserAsync(user.Id, now, cancellationToken);

        events.Record("PasswordResetCompleted", user.TenantId, nameof(User), user.Id.ToString(), new { }, actorId: user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
