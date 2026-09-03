using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Procurement;

public sealed class PurchaseOrderRepository(AppDbContext context) : IPurchaseOrderRepository
{
    public async Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken) =>
        await context.PurchaseOrders.AddAsync(order, cancellationToken);

    public Task<PurchaseOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.PurchaseOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public async Task<PagedResult<PurchaseOrder>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.PurchaseOrders.Include(o => o.Lines).Where(o => o.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrder> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task<bool> ExistsForRequestAsync(Guid tenantId, Guid purchaseRequestId, CancellationToken cancellationToken) =>
        context.PurchaseOrders.AnyAsync(o => o.TenantId == tenantId && o.PurchaseRequestId == purchaseRequestId, cancellationToken);

    public Task<bool> PoNumberExistsAsync(Guid tenantId, string poNumber, CancellationToken cancellationToken) =>
        context.PurchaseOrders.AnyAsync(o => o.TenantId == tenantId && o.PoNumber == poNumber, cancellationToken);
}
