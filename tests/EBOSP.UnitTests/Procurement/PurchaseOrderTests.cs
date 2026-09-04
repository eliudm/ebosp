using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Procurement;

public class PurchaseOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ComputesTotalFromLines()
    {
        var order = PurchaseOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PO-1001", Guid.NewGuid(), Now,
            [(Guid.NewGuid(), 10, 5m), (Guid.NewGuid(), 3, 2m)]);

        Assert.Equal(56m, order.Total);
        Assert.Equal(PurchaseOrderStatus.Open, order.Status);
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => PurchaseOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PO-1001", Guid.NewGuid(), Now,
            Array.Empty<(Guid, int, decimal)>()));
    }

    [Fact]
    public void Create_BlankPoNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => PurchaseOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ", Guid.NewGuid(), Now,
            [(Guid.NewGuid(), 1, 1m)]));
    }
}
