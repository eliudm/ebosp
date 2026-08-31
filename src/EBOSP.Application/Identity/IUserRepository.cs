using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up a user by id or email without applying the tenant query filter. Only valid before
    /// authentication (login, refresh), when the caller's tenant is not yet known.
    /// </summary>
    Task<User?> GetByIdIgnoringTenantAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
}
