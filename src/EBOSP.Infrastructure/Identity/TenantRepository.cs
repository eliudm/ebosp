using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Identity;

public sealed class TenantRepository(AppDbContext context) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken) =>
        await context.Tenants.AddAsync(tenant, cancellationToken);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
}
