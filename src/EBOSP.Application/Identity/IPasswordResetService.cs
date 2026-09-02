using EBOSP.Contracts.Identity;

namespace EBOSP.Application.Identity;

public interface IPasswordResetService
{
    /// <summary>Always succeeds regardless of whether the email is registered (spec §11: no account enumeration).</summary>
    Task RequestResetAsync(PasswordResetRequest request, CancellationToken cancellationToken);

    Task ConfirmResetAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken);
}
