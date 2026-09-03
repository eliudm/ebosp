using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Inventory;

public class StockBalanceTests
{
    private static StockBalance NewBalance() => StockBalance.CreateEmpty(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Receive_IncreasesOnHand()
    {
        var balance = NewBalance();

        balance.Receive(100);

        Assert.Equal(100, balance.QuantityOnHand);
        Assert.Equal(100, balance.QuantityAvailable);
    }

    [Fact]
    public void Issue_DecreasesOnHand()
    {
        var balance = NewBalance();
        balance.Receive(100);

        balance.Issue(20);

        Assert.Equal(80, balance.QuantityOnHand);
    }

    [Fact]
    public void Issue_MoreThanAvailable_Throws()
    {
        var balance = NewBalance();
        balance.Receive(10);

        Assert.Throws<InvalidOperationException>(() => balance.Issue(11));
        Assert.Equal(10, balance.QuantityOnHand);
    }

    [Fact]
    public void Reserve_ReducesAvailable_ButNotOnHand()
    {
        var balance = NewBalance();
        balance.Receive(100);

        balance.Reserve(30);

        Assert.Equal(100, balance.QuantityOnHand);
        Assert.Equal(30, balance.QuantityReserved);
        Assert.Equal(70, balance.QuantityAvailable);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Throws()
    {
        var balance = NewBalance();
        balance.Receive(10);

        Assert.Throws<InvalidOperationException>(() => balance.Reserve(11));
    }

    [Fact]
    public void Release_ReturnsReservedToAvailable()
    {
        var balance = NewBalance();
        balance.Receive(100);
        balance.Reserve(30);

        balance.Release(30);

        Assert.Equal(0, balance.QuantityReserved);
        Assert.Equal(100, balance.QuantityAvailable);
    }

    [Fact]
    public void Release_MoreThanReserved_Throws()
    {
        var balance = NewBalance();
        balance.Receive(100);
        balance.Reserve(10);

        Assert.Throws<InvalidOperationException>(() => balance.Release(11));
    }

    [Fact]
    public void AdjustBy_Positive_IncreasesOnHand()
    {
        var balance = NewBalance();
        balance.Receive(50);

        balance.AdjustBy(5);

        Assert.Equal(55, balance.QuantityOnHand);
    }

    [Fact]
    public void AdjustBy_NegativeBeyondOnHand_Throws()
    {
        var balance = NewBalance();
        balance.Receive(5);

        Assert.Throws<InvalidOperationException>(() => balance.AdjustBy(-6));
        Assert.Equal(5, balance.QuantityOnHand);
    }

    [Fact]
    public void AdjustBy_Zero_Throws()
    {
        var balance = NewBalance();

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.AdjustBy(0));
    }

    [Fact]
    public void Issue_ZeroOrNegativeQuantity_Throws()
    {
        var balance = NewBalance();
        balance.Receive(10);

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.Issue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.Issue(-1));
    }

    [Fact]
    public void Receive_ThenReceiveAgain_OverflowingOnHand_ThrowsWithoutCorruptingBalance()
    {
        var balance = NewBalance();
        balance.Receive(int.MaxValue);

        Assert.Throws<InvalidOperationException>(() => balance.Receive(1));
        Assert.Equal(int.MaxValue, balance.QuantityOnHand);
    }

    [Fact]
    public void AdjustBy_OverflowingOnHand_ThrowsWithoutCorruptingBalance()
    {
        var balance = NewBalance();
        balance.Receive(int.MaxValue - 1);

        Assert.Throws<InvalidOperationException>(() => balance.AdjustBy(10));
        Assert.Equal(int.MaxValue - 1, balance.QuantityOnHand);
    }
}
