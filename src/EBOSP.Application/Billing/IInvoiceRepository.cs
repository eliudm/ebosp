using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Billing;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Invoice>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> ExistsForSalesOrderAsync(Guid tenantId, Guid salesOrderId, CancellationToken cancellationToken);

    /// <summary>Refreshes a tracked invoice to its current database values after a concurrency conflict, so the caller can safely reapply its mutation and retry.</summary>
    Task ReloadAsync(Invoice invoice, CancellationToken cancellationToken);
}
