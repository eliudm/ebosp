using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum InvoiceStatus
{
    Issued,
    Paid,
    Cancelled,
}

/// <summary>
/// Billable document (ERD: InvoiceId, CustomerId/SupplierId, Total, Status; dev guide §15).
/// Total is copied from the already-fulfilled SalesOrder, itself locked in from the customer-
/// accepted Quotation - no InvoiceLine entity, since no new pricing decision happens at this step.
/// Concurrency is enforced at the EF Core mapping level via Postgres's xmin system column, same as
/// StockBalance - two payments confirmed concurrently against the same invoice must not each
/// compute PaidTotal from a stale snapshot and leave a fully-paid invoice stuck at Issued.
/// </summary>
public sealed class Invoice : Entity, ITenantOwned
{
    private Invoice()
    {
    }

    public static Invoice Create(Guid tenantId, Guid salesOrderId, Guid customerId, decimal total, Guid createdByUserId, DateTimeOffset createdAt)
    {
        if (total < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Invoice total cannot be negative.");
        }

        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            CustomerId = customerId,
            Total = total,
            Status = InvoiceStatus.Issued,
            PaidTotal = 0m,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid SalesOrderId { get; private init; }

    public Guid CustomerId { get; private init; }

    public decimal Total { get; private init; }

    public InvoiceStatus Status { get; private set; }

    public decimal PaidTotal { get; private set; }

    public Guid CreatedByUserId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public void MarkPaid()
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new InvalidOperationException("Only an issued invoice can be marked paid.");
        }

        Status = InvoiceStatus.Paid;
    }

    /// <summary>
    /// Applies a confirmed payment's amount against PaidTotal and marks the invoice Paid once it
    /// exactly covers Total. Always mutates PaidTotal (even when the invoice doesn't reach Paid
    /// yet), so the entity is guaranteed to participate in every SaveChanges call it's part of -
    /// required for the xmin concurrency check to actually fire on a concurrent second payment.
    /// </summary>
    public void RecordPayment(decimal amount)
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new InvalidOperationException("Only an issued invoice can receive a payment.");
        }

        var newPaidTotal = PaidTotal + amount;
        if (newPaidTotal > Total)
        {
            throw new InvalidOperationException("This payment would exceed the invoice total.");
        }

        PaidTotal = newPaidTotal;
        if (PaidTotal == Total)
        {
            MarkPaid();
        }
    }
}
