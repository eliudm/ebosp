using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Billing;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Invoice>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> ExistsForSalesOrderAsync(Guid tenantId, Guid salesOrderId, CancellationToken cancellationToken);
}
