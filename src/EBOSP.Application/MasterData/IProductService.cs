using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;

namespace EBOSP.Application.MasterData;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
