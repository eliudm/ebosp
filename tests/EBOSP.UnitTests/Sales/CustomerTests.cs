using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Sales;

public class CustomerTests
{
    [Fact]
    public void Create_NegativeCreditLimit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Customer.Create(Guid.NewGuid(), "Acme", -1m));
    }

    [Fact]
    public void Update_BlankName_Throws()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Acme", 1000m);

        Assert.Throws<ArgumentException>(() => customer.Update(" ", 1000m));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Acme", 1000m);

        customer.Deactivate();
        Assert.Equal(CustomerStatus.Inactive, customer.Status);

        customer.Activate();
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }
}
