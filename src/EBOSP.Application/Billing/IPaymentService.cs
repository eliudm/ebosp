using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;

namespace EBOSP.Application.Billing;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentResponse> ConfirmAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<PaymentResponse> FailAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<PaymentResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PaymentResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
