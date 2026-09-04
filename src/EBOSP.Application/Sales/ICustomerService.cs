using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;

namespace EBOSP.Application.Sales;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateCustomerRequest request, CancellationToken cancellationToken);

    Task<CustomerResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<CustomerResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CustomerResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
