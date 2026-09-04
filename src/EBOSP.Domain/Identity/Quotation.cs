using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum QuotationStatus
{
    Pending,
    Accepted,
    Rejected,
}

/// <summary>
/// Sales quotation (dev guide §15's flow names it as the first step; not in the ERD, which only
/// lists SalesOrder). Accepting/rejecting records the *customer's* decision, relayed by a Sales
/// Officer - not an internal approval with a segregation-of-duties concern like PurchaseRequest,
/// so there's no WorkflowInstance and no self-action guard here.
/// </summary>
public sealed class Quotation : Entity, ITenantOwned
{
    private readonly List<QuotationLine> _lines = [];

    private Quotation()
    {
    }

    public static Quotation Create(
        Guid tenantId,
        Guid customerId,
        Guid branchId,
        IReadOnlyCollection<(Guid ProductId, int Quantity, decimal UnitPrice)> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A quotation must have at least one line.", nameof(lines));
        }

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            BranchId = branchId,
            Status = QuotationStatus.Pending,
        };

        foreach (var (productId, quantity, unitPrice) in lines)
        {
            quotation._lines.Add(QuotationLine.Create(tenantId, quotation.Id, productId, quantity, unitPrice));
        }

        quotation.Total = quotation._lines.Sum(l => l.LineTotal);

        return quotation;
    }

    public Guid TenantId { get; private init; }

    public Guid CustomerId { get; private init; }

    public Guid BranchId { get; private init; }

    public decimal Total { get; private set; }

    public QuotationStatus Status { get; private set; }

    public IReadOnlyList<QuotationLine> Lines => _lines;

    public void Accept()
    {
        if (Status != QuotationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending quotation can be accepted.");
        }

        Status = QuotationStatus.Accepted;
    }

    public void Reject()
    {
        if (Status != QuotationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending quotation can be rejected.");
        }

        Status = QuotationStatus.Rejected;
    }
}
