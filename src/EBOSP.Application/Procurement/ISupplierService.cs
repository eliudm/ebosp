using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;

namespace EBOSP.Application.Procurement;

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateSupplierRequest request, CancellationToken cancellationToken);

    Task<SupplierResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<SupplierResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SupplierResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
