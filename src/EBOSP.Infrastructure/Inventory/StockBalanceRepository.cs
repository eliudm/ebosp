using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class StockBalanceRepository(AppDbContext context) : IStockBalanceRepository
{
    public Task<StockBalance?> GetAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken) =>
        context.StockBalances.SingleOrDefaultAsync(b => b.TenantId == tenantId && b.WarehouseId == warehouseId && b.ProductId == productId, cancellationToken);

    public async Task AddAsync(StockBalance balance, CancellationToken cancellationToken) =>
        await context.StockBalances.AddAsync(balance, cancellationToken);

    public async Task<PagedResult<StockBalance>> ListAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.StockBalances.Where(b => b.TenantId == tenantId);
        if (warehouseId is { } w)
        {
            query = query.Where(b => b.WarehouseId == w);
        }

        if (productId is { } p)
        {
            query = query.Where(b => b.ProductId == p);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = request.SortDescending
            ? query.OrderByDescending(b => b.WarehouseId).ThenByDescending(b => b.ProductId)
            : query.OrderBy(b => b.WarehouseId).ThenBy(b => b.ProductId);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<StockBalance> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task ReloadAsync(StockBalance balance, CancellationToken cancellationToken) =>
        context.Entry(balance).ReloadAsync(cancellationToken);
}
