using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Procurement;

public sealed class SupplierRepository(AppDbContext context) : ISupplierRepository
{
    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken) =>
        await context.Suppliers.AddAsync(supplier, cancellationToken);

    public Task<Supplier?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Suppliers.SingleOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id, cancellationToken);

    public async Task<PagedResult<Supplier>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Suppliers.Where(s => s.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Supplier> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
