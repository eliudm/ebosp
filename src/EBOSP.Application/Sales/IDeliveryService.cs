using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;

namespace EBOSP.Application.Sales;

public interface IDeliveryService
{
    Task<DeliveryResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateDeliveryRequest request, CancellationToken cancellationToken);

    Task<DeliveryResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DeliveryResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
