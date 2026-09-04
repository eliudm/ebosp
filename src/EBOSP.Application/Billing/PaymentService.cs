using EBOSP.Application.Common;
using EBOSP.Application.Security;
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
    ISecurityAlertRepository securityAlerts,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    BillingOptions options) : IPaymentService
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

        var now = clock.UtcNow;
        payment.Confirm(actingUserId, now);
        events.Record("PaymentReceived", tenantId, nameof(Payment), payment.Id.ToString(), new { payment.InvoiceId, payment.Amount }, actorId: actingUserId);

        // The caller-side permission check (payment.create.large) already happened before this
        // method was invoked - this is the corresponding "+ alert" half of spec §16's "Unusual
        // payment... Finance review" response, same split as InventoryService.AdjustAsync's
        // StockAdjustmentFlagged/LargeStockAdjustment alert.
        if (payment.Amount > options.LargePaymentThreshold)
        {
            await securityAlerts.AddAsync(
                SecurityAlert.Raise(
                    tenantId,
                    "UnusualPayment",
                    SecurityAlertSeverity.Critical,
                    $"Payment of {payment.Amount} exceeded the configured threshold.",
                    now,
                    relatedActorId: actingUserId,
                    relatedAggregateType: nameof(Payment),
                    relatedAggregateId: payment.Id.ToString()),
                cancellationToken);
        }

        // Invoice.PaidTotal is guarded by Postgres's xmin as a real EF Core concurrency token (same
        // mechanism as StockBalance in M4) - two payments confirmed concurrently against the same
        // invoice would otherwise each compute PaidTotal from a stale snapshot and could leave a
        // fully-paid invoice permanently stuck at Issued, with no further payment able to correct it
        // (any additional amount would now look like an overpayment). The loser here reloads the
        // now-current PaidTotal and reapplies RecordPayment against it instead.
        await SaveWithConcurrencyRetryAsync(invoice, () => invoice.RecordPayment(payment.Amount), cancellationToken);

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

    /// <summary>Mirrors InventoryService.SaveWithConcurrencyRetryAsync's shape - reapplies the mutation and saves, retrying with a fresh reload of the invoice on a concurrency conflict.</summary>
    private async Task SaveWithConcurrencyRetryAsync(Invoice invoice, Action reapplyMutation, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                reapplyMutation();
            }
            catch (InvalidOperationException ex)
            {
                throw new ConflictException(ex.Message);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxAttempts)
            {
                await invoices.ReloadAsync(invoice, cancellationToken);
                await Task.Delay(Random.Shared.Next(5, 5 * attempt), cancellationToken);
            }
        }
    }

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.Id, payment.InvoiceId, payment.Amount, payment.Method, payment.Status.ToString(), payment.CreatedByUserId, payment.CreatedAt, payment.ConfirmedByUserId, payment.ConfirmedAt);
}
