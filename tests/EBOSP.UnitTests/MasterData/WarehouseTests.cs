using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.MasterData;

public class WarehouseTests
{
    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Warehouse.Create(Guid.NewGuid(), Guid.NewGuid(), " "));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var warehouse = Warehouse.Create(Guid.NewGuid(), Guid.NewGuid(), "Main Warehouse");

        warehouse.Deactivate();
        Assert.Equal(WarehouseStatus.Inactive, warehouse.Status);

        warehouse.Activate();
        Assert.Equal(WarehouseStatus.Active, warehouse.Status);
    }

    [Fact]
    public void Update_TrimsAndSetsName()
    {
        var warehouse = Warehouse.Create(Guid.NewGuid(), Guid.NewGuid(), "Main Warehouse");

        warehouse.Update("  Renamed Warehouse  ");

        Assert.Equal("Renamed Warehouse", warehouse.Name);
    }
}
