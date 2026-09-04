using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Billing;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    Task<Payment?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Payment>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Sum of Amount across the invoice's Successful payments - the overpayment guard used by PaymentService.ConfirmAsync.</summary>
    Task<decimal> SumSuccessfulAmountForInvoiceAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken);
}
