using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.MasterData;

public sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task AddAsync(Product product, CancellationToken cancellationToken) =>
        await context.Products.AddAsync(product, cancellationToken);

    public Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Products.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken);

    public async Task<PagedResult<Product>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Products.Where(p => p.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Product> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task<bool> SkuExistsAsync(Guid tenantId, string sku, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(p => p.TenantId == tenantId && p.Sku == sku, cancellationToken);
}
