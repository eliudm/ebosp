using EBOSP.Application.Common;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Billing;

/// <summary>
/// Records payments against an Invoice (dev guide §16). A payment always starts Pending -
/// ConfirmAsync/FailAsync are the only paths that can ever settle it, so the client never
/// supplies a "paid" flag directly.
/// </summary>
public sealed class PaymentService(
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IPaymentService
{
    public async Task<PaymentResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key && await payments.IdempotencyKeyExistsAsync(tenantId, key, cancellationToken))
        {
            throw new ConflictException("This payment has already been processed.");
        }

        var invoice = await invoices.GetByIdAsync(tenantId, request.InvoiceId, cancellationToken)
                      ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new ConflictException("This invoice is not open for payment.");
        }

        var payment = Payment.Create(tenantId, invoice.Id, request.Amount, request.Method, actingUserId, clock.UtcNow, request.IdempotencyKey);
        await payments.AddAsync(payment, cancellationToken);

        events.Record("PaymentRequested", tenantId, nameof(Payment), payment.Id.ToString(), new { payment.InvoiceId, payment.Amount, payment.Method }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(payment);
    }

    public async Task<PaymentResponse> ConfirmAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var payment = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ConflictException("Only a pending payment can be confirmed.");
        }

        var invoice = await invoices.GetByIdAsync(tenantId, payment.InvoiceId, cancellationToken)
                      ?? throw new NotFoundException("Invoice not found.");

        // Accepted, narrow race (documented, same as M6's credit-limit check): two concurrent
        // ConfirmAsync calls against different Pending payments on the same invoice could each read
        // this sum before either commits and both pass under the total. Fixing it would need
        // row-level locking, a pattern not used anywhere else in this codebase, for a check neither
        // governing doc requires be strictly serialized.
        var alreadySuccessful = await payments.SumSuccessfulAmountForInvoiceAsync(tenantId, invoice.Id, cancellationToken);
        if (alreadySuccessful + payment.Amount > invoice.Total)
        {
            throw new ConflictException("This payment would exceed the invoice total.");
        }

        var now = clock.UtcNow;
        payment.Confirm(actingUserId, now);
        if (alreadySuccessful + payment.Amount == invoice.Total)
        {
            invoice.MarkPaid();
        }

        events.Record("PaymentReceived", tenantId, nameof(Payment), payment.Id.ToString(), new { payment.InvoiceId, payment.Amount }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(payment);
    }

    public async Task<PaymentResponse> FailAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var payment = await GetOwnedAsync(tenantId, id, cancellationToken);
        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ConflictException("Only a pending payment can be failed.");
        }

        payment.Fail(actingUserId, clock.UtcNow);

        events.Record("PaymentFailed", tenantId, nameof(Payment), payment.Id.ToString(), new { payment.InvoiceId, payment.Amount }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(payment);
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<PaymentResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await payments.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<PaymentResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Payment> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await payments.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Payment not found.");

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.Id, payment.InvoiceId, payment.Amount, payment.Method, payment.Status.ToString(), payment.CreatedByUserId, payment.CreatedAt, payment.ConfirmedByUserId, payment.ConfirmedAt);
}
