using EBOSP.Application.Identity;
using Microsoft.Extensions.Logging;

namespace EBOSP.Infrastructure.Identity;

/// <summary>
/// Placeholder <see cref="IPasswordResetNotifier"/> until the Notifications module (dev guide
/// §17) provides a real email/SMS adapter. Deliberately never logs the token value itself (dev
/// guide §14: "Log security-relevant actions without logging passwords, tokens or unnecessary
/// sensitive data") - it logs that a reset was requested, which is enough to prove the seam works
/// without creating a log-based way to hijack an account.
/// </summary>
public sealed class LoggingPasswordResetNotifier(ILogger<LoggingPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        logger.LogInformation("Password reset requested for {Email} - no notification channel is wired up yet (dev guide §17).", email);
        return Task.CompletedTask;
    }
}
