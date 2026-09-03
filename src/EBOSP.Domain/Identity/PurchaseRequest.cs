using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum PurchaseRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>
/// Purchase request (ERD: RequestId, RequestedBy, Amount, Status; dev guide §14). Status is a
/// denormalized fast-read copy of its linked WorkflowInstance's status - the same
/// projection-plus-immutable-history split already used for StockBalance/StockLedgerEntry, where
/// the outbox (PurchaseRequestSubmitted/PurchaseApproved/PurchaseRequestRejected) is the history.
/// </summary>
public sealed class PurchaseRequest : Entity, ITenantOwned
{
    private readonly List<PurchaseRequestLine> _lines = [];

    private PurchaseRequest()
    {
    }

    public static PurchaseRequest Create(
        Guid tenantId,
        Guid requestedByUserId,
        Guid branchId,
        string justification,
        DateTimeOffset requiredDate,
        IReadOnlyCollection<(Guid ProductId, int Quantity, decimal EstimatedUnitPrice)> lines)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new ArgumentException("A justification is required for a purchase request.", nameof(justification));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("A purchase request must have at least one line.", nameof(lines));
        }

        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestedByUserId = requestedByUserId,
            BranchId = branchId,
            Justification = justification.Trim(),
            RequiredDate = requiredDate,
            Status = PurchaseRequestStatus.Pending,
        };

        foreach (var (productId, quantity, estimatedUnitPrice) in lines)
        {
            request._lines.Add(PurchaseRequestLine.Create(tenantId, request.Id, productId, quantity, estimatedUnitPrice));
        }

        request.EstimatedValue = request._lines.Sum(l => l.EstimatedLineTotal);

        return request;
    }

    public Guid TenantId { get; private init; }

    public Guid RequestedByUserId { get; private init; }

    public Guid BranchId { get; private init; }

    public string Justification { get; private init; } = null!;

    public decimal EstimatedValue { get; private set; }

    public DateTimeOffset RequiredDate { get; private init; }

    public PurchaseRequestStatus Status { get; private set; }

    public IReadOnlyList<PurchaseRequestLine> Lines => _lines;

    public void MarkApproved()
    {
        if (Status != PurchaseRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending purchase request can be approved.");
        }

        Status = PurchaseRequestStatus.Approved;
    }

    public void MarkRejected()
    {
        if (Status != PurchaseRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending purchase request can be rejected.");
        }

        Status = PurchaseRequestStatus.Rejected;
    }
}
