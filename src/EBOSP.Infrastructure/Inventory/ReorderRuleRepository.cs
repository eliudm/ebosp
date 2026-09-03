using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class ReorderRuleRepository(AppDbContext context) : IReorderRuleRepository
{
    public async Task AddAsync(ReorderRule rule, CancellationToken cancellationToken) =>
        await context.ReorderRules.AddAsync(rule, cancellationToken);

    public Task<ReorderRule?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.ReorderRules.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken);

    public Task<ReorderRule?> FindForAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken) =>
        context.ReorderRules.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.WarehouseId == warehouseId && r.ProductId == productId, cancellationToken);

    public async Task<PagedResult<ReorderRule>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.ReorderRules.Where(r => r.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(r => r.WarehouseId) : query.OrderBy(r => r.WarehouseId);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<ReorderRule> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public void Remove(ReorderRule rule) => context.ReorderRules.Remove(rule);
}
