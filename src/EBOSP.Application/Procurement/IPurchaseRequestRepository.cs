using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

public interface IPurchaseRequestRepository
{
    Task AddAsync(PurchaseRequest request, CancellationToken cancellationToken);

    Task<PurchaseRequest?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseRequest>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
