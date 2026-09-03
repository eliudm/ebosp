using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;

namespace EBOSP.Application.Procurement;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken);

    Task<PurchaseOrderResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseOrderResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
