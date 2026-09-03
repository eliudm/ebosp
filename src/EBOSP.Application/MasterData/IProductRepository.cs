using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Product>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(Guid tenantId, string sku, CancellationToken cancellationToken);
}
