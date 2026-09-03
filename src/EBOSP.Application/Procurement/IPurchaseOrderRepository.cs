using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Procurement;

public interface IPurchaseOrderRepository
{
    Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken);

    Task<PurchaseOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseOrder>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> ExistsForRequestAsync(Guid tenantId, Guid purchaseRequestId, CancellationToken cancellationToken);

    Task<bool> PoNumberExistsAsync(Guid tenantId, string poNumber, CancellationToken cancellationToken);
}
