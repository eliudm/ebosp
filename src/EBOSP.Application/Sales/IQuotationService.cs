using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;

namespace EBOSP.Application.Sales;

public interface IQuotationService
{
    Task<QuotationResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateQuotationRequest request, CancellationToken cancellationToken);

    Task<QuotationResponse> AcceptAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<QuotationResponse> RejectAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<QuotationResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<QuotationResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
