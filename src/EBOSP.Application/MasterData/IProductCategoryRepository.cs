using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.MasterData;

public interface IProductCategoryRepository
{
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken);

    Task<ProductCategory?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductCategory>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
