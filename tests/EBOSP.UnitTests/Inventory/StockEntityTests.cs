using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Inventory;

public class StockEntityTests
{
    [Fact]
    public void StockAdjustment_Create_BlankReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StockAdjustment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, " ", Guid.NewGuid(), DateTimeOffset.UtcNow, null));
    }

    [Fact]
    public void StockAdjustment_Create_ZeroDelta_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StockAdjustment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "Recount", Guid.NewGuid(), DateTimeOffset.UtcNow, null));
    }

    [Fact]
    public void StockTransfer_Create_SameWarehouseTwice_Throws()
    {
        var warehouseId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            StockTransfer.Create(Guid.NewGuid(), warehouseId, warehouseId, Guid.NewGuid(), 10, null, Guid.NewGuid(), DateTimeOffset.UtcNow, null));
    }

    [Fact]
    public void StockReservation_Release_TwiceThrows()
    {
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, DateTimeOffset.UtcNow);

        reservation.Release(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => reservation.Release(DateTimeOffset.UtcNow));
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void ReorderRule_Create_NegativeLevel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReorderRule.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1));
    }

    [Fact]
    public void GoodsReceipt_Create_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            GoodsReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null, null, null, []));
    }
}
