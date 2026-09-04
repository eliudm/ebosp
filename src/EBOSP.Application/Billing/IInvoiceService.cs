using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;

namespace EBOSP.Application.Billing;

public interface IInvoiceService
{
    Task<InvoiceResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateInvoiceRequest request, CancellationToken cancellationToken);

    Task<InvoiceResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
