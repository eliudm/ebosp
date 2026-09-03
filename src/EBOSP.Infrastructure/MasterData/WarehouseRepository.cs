using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.MasterData;

public sealed class WarehouseRepository(AppDbContext context) : IWarehouseRepository
{
    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken) =>
        await context.Warehouses.AddAsync(warehouse, cancellationToken);

    public Task<Warehouse?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Warehouses.SingleOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken);

    public async Task<PagedResult<Warehouse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Warehouses.Where(w => w.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Warehouse> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
