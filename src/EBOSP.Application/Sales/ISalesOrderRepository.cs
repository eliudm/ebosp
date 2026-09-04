using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public interface ISalesOrderRepository
{
    Task AddAsync(SalesOrder order, CancellationToken cancellationToken);

    Task<SalesOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SalesOrder>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> ExistsForQuotationAsync(Guid tenantId, Guid quotationId, CancellationToken cancellationToken);

    /// <summary>Sum of Total across the customer's non-cancelled orders - the credit-exposure signal used by SalesOrderService's credit check.</summary>
    Task<decimal> SumOpenTotalsForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
}
