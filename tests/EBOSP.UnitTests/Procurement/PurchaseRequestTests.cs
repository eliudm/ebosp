using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Procurement;

public class PurchaseRequestTests
{
    private static readonly DateTimeOffset RequiredDate = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static PurchaseRequest NewRequest(params (Guid ProductId, int Quantity, decimal EstimatedUnitPrice)[] lines) =>
        PurchaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Restock", RequiredDate, lines);

    [Fact]
    public void Create_ComputesEstimatedValueFromLines()
    {
        var request = NewRequest((Guid.NewGuid(), 10, 5m), (Guid.NewGuid(), 4, 2.5m));

        Assert.Equal(60m, request.EstimatedValue);
        Assert.Equal(PurchaseRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => PurchaseRequest.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Restock", RequiredDate,
            Array.Empty<(Guid, int, decimal)>()));
    }

    [Fact]
    public void Create_BlankJustification_Throws()
    {
        Assert.Throws<ArgumentException>(() => PurchaseRequest.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ", RequiredDate,
            [(Guid.NewGuid(), 1, 1m)]));
    }

    [Fact]
    public void MarkApproved_PendingRequest_TransitionsToApproved()
    {
        var request = NewRequest((Guid.NewGuid(), 1, 1m));

        request.MarkApproved();

        Assert.Equal(PurchaseRequestStatus.Approved, request.Status);
    }

    [Fact]
    public void MarkRejected_PendingRequest_TransitionsToRejected()
    {
        var request = NewRequest((Guid.NewGuid(), 1, 1m));

        request.MarkRejected();

        Assert.Equal(PurchaseRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void MarkApproved_AlreadyDecided_Throws()
    {
        var request = NewRequest((Guid.NewGuid(), 1, 1m));
        request.MarkApproved();

        Assert.Throws<InvalidOperationException>(() => request.MarkApproved());
    }

    [Fact]
    public void MarkRejected_AlreadyApproved_Throws()
    {
        var request = NewRequest((Guid.NewGuid(), 1, 1m));
        request.MarkApproved();

        Assert.Throws<InvalidOperationException>(() => request.MarkRejected());
    }
}
