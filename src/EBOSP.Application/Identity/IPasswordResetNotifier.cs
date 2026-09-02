namespace EBOSP.Application.Identity;

/// <summary>
/// Delivers the password reset token to the user. Placeholder seam until the Notifications module
/// (dev guide §17, a later milestone) provides a real email/SMS adapter - today's implementation
/// only logs, so requesting a reset in this pass does not actually deliver anything to the user.
/// </summary>
public interface IPasswordResetNotifier
{
    Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken);
}
