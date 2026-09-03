using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.MasterData;

public sealed class BranchRepository(AppDbContext context) : IBranchRepository
{
    public async Task AddAsync(Branch branch, CancellationToken cancellationToken) =>
        await context.Branches.AddAsync(branch, cancellationToken);

    public Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Branches.SingleOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, cancellationToken);

    public async Task<PagedResult<Branch>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Branches.Where(b => b.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Branch> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
