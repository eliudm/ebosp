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
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid SalesOrderId { get; private init; }

    public Guid CustomerId { get; private init; }

    public decimal Total { get; private init; }

    public InvoiceStatus Status { get; private set; }

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
}
