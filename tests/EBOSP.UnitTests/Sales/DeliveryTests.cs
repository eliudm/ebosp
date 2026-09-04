using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Sales;

public class DeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CopiesLines()
    {
        var productId = Guid.NewGuid();

        var delivery = Delivery.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now, [(productId, 5)]);

        Assert.Single(delivery.Lines);
        Assert.Equal(productId, delivery.Lines[0].ProductId);
        Assert.Equal(5, delivery.Lines[0].Quantity);
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => Delivery.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now, Array.Empty<(Guid, int)>()));
    }
}
