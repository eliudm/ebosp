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

    [Fact]
    public void RecordPayment_PartialAmount_AccumulatesWithoutMarkingPaid()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);

        invoice.RecordPayment(60m);

        Assert.Equal(60m, invoice.PaidTotal);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void RecordPayment_ReachesExactTotalAcrossTwoCalls_MarksPaid()
    {
        // Simulates the sequential (non-racing) split-payment case: two payments that together
        // exactly cover the total, applied one after another against the same in-memory instance.
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);

        invoice.RecordPayment(60m);
        invoice.RecordPayment(40m);

        Assert.Equal(100m, invoice.PaidTotal);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void RecordPayment_WouldExceedTotal_ThrowsAndDoesNotMutate()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);
        invoice.RecordPayment(60m);

        Assert.Throws<InvalidOperationException>(() => invoice.RecordPayment(60m));
        Assert.Equal(60m, invoice.PaidTotal);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void RecordPayment_AlreadyPaid_Throws()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), Now);
        invoice.RecordPayment(100m);

        Assert.Throws<InvalidOperationException>(() => invoice.RecordPayment(1m));
    }
}
