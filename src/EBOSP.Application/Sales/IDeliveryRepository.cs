using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public interface IDeliveryRepository
{
    Task AddAsync(Delivery delivery, CancellationToken cancellationToken);

    Task<Delivery?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Delivery>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
