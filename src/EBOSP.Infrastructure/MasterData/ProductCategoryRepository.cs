using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.MasterData;

public sealed class ProductCategoryRepository(AppDbContext context) : IProductCategoryRepository
{
    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken) =>
        await context.ProductCategories.AddAsync(category, cancellationToken);

    public Task<ProductCategory?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.ProductCategories.SingleOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

    public async Task<PagedResult<ProductCategory>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.ProductCategories.Where(c => c.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<ProductCategory> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
