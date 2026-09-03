using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Procurement;

public sealed class PurchaseRequestRepository(AppDbContext context) : IPurchaseRequestRepository
{
    public async Task AddAsync(PurchaseRequest request, CancellationToken cancellationToken) =>
        await context.PurchaseRequests.AddAsync(request, cancellationToken);

    public Task<PurchaseRequest?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.PurchaseRequests.Include(r => r.Lines).SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken);

    public async Task<PagedResult<PurchaseRequest>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.PurchaseRequests.Include(r => r.Lines).Where(r => r.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(r => r.RequiredDate) : query.OrderBy(r => r.RequiredDate);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<PurchaseRequest> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
