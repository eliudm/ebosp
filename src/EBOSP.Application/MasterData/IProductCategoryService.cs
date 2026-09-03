using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;

namespace EBOSP.Application.MasterData;

public interface IProductCategoryService
{
    Task<ProductCategoryResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateProductCategoryRequest request, CancellationToken cancellationToken);

    Task<ProductCategoryResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateProductCategoryRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<ProductCategoryResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductCategoryResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
