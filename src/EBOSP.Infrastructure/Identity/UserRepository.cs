using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Identity;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);

    public Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(u => u.TenantId == tenantId && u.Id == id, cancellationToken);

    public Task<User?> GetByIdIgnoringTenantAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken) =>
        context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);
}
