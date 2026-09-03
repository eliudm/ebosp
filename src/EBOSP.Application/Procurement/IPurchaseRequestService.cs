using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;

namespace EBOSP.Application.Procurement;

public interface IPurchaseRequestService
{
    Task<PurchaseRequestResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePurchaseRequestRequest request, CancellationToken cancellationToken);

    Task<PurchaseRequestResponse> ApproveAsync(Guid tenantId, Guid actingUserId, Guid id, ApprovePurchaseRequestRequest request, CancellationToken cancellationToken);

    Task<PurchaseRequestResponse> RejectAsync(Guid tenantId, Guid actingUserId, Guid id, RejectPurchaseRequestRequest request, CancellationToken cancellationToken);

    Task<PurchaseRequestResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PurchaseRequestResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
