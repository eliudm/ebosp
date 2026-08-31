using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Identity;

public sealed class RoleRepository(AppDbContext context) : IRoleRepository
{
    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Roles.SingleOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task AssignRoleAsync(UserRole userRole, CancellationToken cancellationToken) =>
        await context.UserRoles.AddAsync(userRole, cancellationToken);

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        await context.UserRoles
            // Called from the login/refresh flow, before the caller is authenticated, so the
            // context's ambient tenant filter (keyed off the not-yet-issued token) must be
            // bypassed in favor of the explicit, already-validated tenantId parameter below.
            .IgnoreQueryFilters()
            .Where(ur => ur.TenantId == tenantId && ur.UserId == userId)
            .Join(context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp.PermissionId)
            .Join(context.Permissions, permissionId => permissionId, p => p.Id, (_, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
}
