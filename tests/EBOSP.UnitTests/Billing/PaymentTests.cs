using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Billing;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Payment NewPayment() =>
        Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "Card", Guid.NewGuid(), Now, null);

    [Fact]
    public void Create_StartsPending()
    {
        var payment = NewPayment();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.ConfirmedByUserId);
        Assert.Null(payment.ConfirmedAt);
    }

    [Fact]
    public void Create_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, "Card", Guid.NewGuid(), Now, null));
    }

    [Fact]
    public void Create_BlankMethod_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "  ", Guid.NewGuid(), Now, null));
    }

    [Fact]
    public void Confirm_PendingPayment_TransitionsToSuccessful()
    {
        var payment = NewPayment();
        var confirmer = Guid.NewGuid();

        payment.Confirm(confirmer, Now);

        Assert.Equal(PaymentStatus.Successful, payment.Status);
        Assert.Equal(confirmer, payment.ConfirmedByUserId);
        Assert.Equal(Now, payment.ConfirmedAt);
    }

    [Fact]
    public void Fail_PendingPayment_TransitionsToFailed()
    {
        var payment = NewPayment();

        payment.Fail(Guid.NewGuid(), Now);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Throws()
    {
        var payment = NewPayment();
        payment.Confirm(Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() => payment.Confirm(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Confirm_AlreadyFailed_Throws()
    {
        var payment = NewPayment();
        payment.Fail(Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() => payment.Confirm(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Fail_AlreadyConfirmed_Throws()
    {
        var payment = NewPayment();
        payment.Confirm(Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() => payment.Fail(Guid.NewGuid(), Now));
    }
}
