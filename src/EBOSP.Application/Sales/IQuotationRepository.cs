using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public interface IQuotationRepository
{
    Task AddAsync(Quotation quotation, CancellationToken cancellationToken);

    Task<Quotation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Quotation>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
