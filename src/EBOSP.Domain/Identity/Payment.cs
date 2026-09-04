using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum PaymentStatus
{
    Pending,
    Successful,
    Failed,
}

/// <summary>
/// Settlement transaction against an Invoice (ERD: PaymentId, InvoiceId, Amount, Method; dev guide
/// §16). Starts Pending - only Confirm/Fail (separate, authorized actions) can settle it, so a
/// client can never supply a "paid" status directly (dev guide §16: "never trust a client-provided
/// 'paid' flag"). Reversed/Refunded is out of scope this phase (dev guide's own wording is
/// conditional: "...where required").
/// </summary>
public sealed class Payment : Entity, ITenantOwned
{
    private Payment()
    {
    }

    public static Payment Create(Guid tenantId, Guid invoiceId, decimal amount, string method, Guid createdByUserId, DateTimeOffset createdAt, string? idempotencyKey)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("Payment method is required.", nameof(method));
        }

        return new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Amount = amount,
            Method = method.Trim(),
            Status = PaymentStatus.Pending,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            IdempotencyKey = idempotencyKey,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid InvoiceId { get; private init; }

    public decimal Amount { get; private init; }

    public string Method { get; private init; } = null!;

    public PaymentStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public string? IdempotencyKey { get; private init; }

    public Guid? ConfirmedByUserId { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public void Confirm(Guid confirmedByUserId, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending payment can be confirmed.");
        }

        Status = PaymentStatus.Successful;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedAt = now;
    }

    public void Fail(Guid failedByUserId, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending payment can be failed.");
        }

        Status = PaymentStatus.Failed;
        ConfirmedByUserId = failedByUserId;
        ConfirmedAt = now;
    }
}
