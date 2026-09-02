using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);

    /// <summary>Looks up by hash without applying the tenant query filter - the caller's tenant is derived from the token itself.</summary>
    Task<PasswordResetToken?> FindByHashIgnoringTenantAsync(string tokenHash, CancellationToken cancellationToken);
}
