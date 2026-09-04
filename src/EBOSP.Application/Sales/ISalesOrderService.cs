using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;

namespace EBOSP.Application.Sales;

public interface ISalesOrderService
{
    Task<SalesOrderResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateSalesOrderRequest request, CancellationToken cancellationToken);

    Task<SalesOrderResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SalesOrderResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
