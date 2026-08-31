using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public interface IRoleRepository
{
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task AssignRoleAsync(UserRole userRole, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
}
