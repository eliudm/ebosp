using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Sales;

public class QuotationTests
{
    private static Quotation NewQuotation(params (Guid ProductId, int Quantity, decimal UnitPrice)[] lines) =>
        Quotation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), lines);

    [Fact]
    public void Create_ComputesTotalFromLines()
    {
        var quotation = NewQuotation((Guid.NewGuid(), 10, 5m), (Guid.NewGuid(), 4, 2.5m));

        Assert.Equal(60m, quotation.Total);
        Assert.Equal(QuotationStatus.Pending, quotation.Status);
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => Quotation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Array.Empty<(Guid, int, decimal)>()));
    }

    [Fact]
    public void Accept_PendingQuotation_TransitionsToAccepted()
    {
        var quotation = NewQuotation((Guid.NewGuid(), 1, 1m));

        quotation.Accept();

        Assert.Equal(QuotationStatus.Accepted, quotation.Status);
    }

    [Fact]
    public void Reject_PendingQuotation_TransitionsToRejected()
    {
        var quotation = NewQuotation((Guid.NewGuid(), 1, 1m));

        quotation.Reject();

        Assert.Equal(QuotationStatus.Rejected, quotation.Status);
    }

    [Fact]
    public void Accept_AlreadyDecided_Throws()
    {
        var quotation = NewQuotation((Guid.NewGuid(), 1, 1m));
        quotation.Accept();

        Assert.Throws<InvalidOperationException>(() => quotation.Accept());
    }

    [Fact]
    public void Reject_AlreadyAccepted_Throws()
    {
        var quotation = NewQuotation((Guid.NewGuid(), 1, 1m));
        quotation.Accept();

        Assert.Throws<InvalidOperationException>(() => quotation.Reject());
    }
}
