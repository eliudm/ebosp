using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Billing;

public class InvoiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StartsIssued()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100m, invoice.Total);
    }

    [Fact]
    public void Create_NegativeTotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1m, Guid.NewGuid(), Now));
    }

    [Fact]
    public void MarkPaid_IssuedInvoice_TransitionsToPaid()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);

        invoice.MarkPaid();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void MarkPaid_AlreadyPaid_Throws()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);
        invoice.MarkPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkPaid());
    }
}
