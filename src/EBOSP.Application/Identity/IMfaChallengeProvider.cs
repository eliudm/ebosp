using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

/// <summary>
/// The MFA-ready seam (spec §11): whether a login for this user must complete a second factor
/// before tokens are issued. A real provider (TOTP, WebAuthn, ...) implements this; today
/// <see cref="NoOpMfaChallengeProvider"/> is registered and never requires one - spec §11:
/// "production MFA should be enabled according to risk and organizational policy."
/// </summary>
public interface IMfaChallengeProvider
{
    Task<bool> IsChallengeRequiredAsync(User user, CancellationToken cancellationToken);
}
