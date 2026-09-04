using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Sales;

public class SalesOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ComputesTotalFromLines()
    {
        var order = SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now,
            [(Guid.NewGuid(), 10, 5m, Guid.NewGuid()), (Guid.NewGuid(), 3, 2m, Guid.NewGuid())]);

        Assert.Equal(56m, order.Total);
        Assert.Equal(SalesOrderStatus.Open, order.Status);
    }

    [Fact]
    public void Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now,
            Array.Empty<(Guid, int, decimal, Guid)>()));
    }

    [Fact]
    public void MarkFulfilled_OpenOrder_TransitionsToFulfilled()
    {
        var order = SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now,
            [(Guid.NewGuid(), 1, 1m, Guid.NewGuid())]);

        order.MarkFulfilled();

        Assert.Equal(SalesOrderStatus.Fulfilled, order.Status);
    }

    [Fact]
    public void MarkFulfilled_AlreadyFulfilled_Throws()
    {
        var order = SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now,
            [(Guid.NewGuid(), 1, 1m, Guid.NewGuid())]);
        order.MarkFulfilled();

        Assert.Throws<InvalidOperationException>(() => order.MarkFulfilled());
    }
}
