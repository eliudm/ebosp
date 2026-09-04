using EBOSP.Application.Security;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Security;

public sealed class SecurityAlertRepository(AppDbContext context) : ISecurityAlertRepository
{
    public async Task AddAsync(SecurityAlert alert, CancellationToken cancellationToken) =>
        await context.SecurityAlerts.AddAsync(alert, cancellationToken);

    public Task<SecurityAlert?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.SecurityAlerts.SingleOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, cancellationToken);

    public async Task<PagedResult<SecurityAlert>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.SecurityAlerts.Where(a => a.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<SecurityAlert> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
