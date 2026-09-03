using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.MasterData;

public class ProductCategoryTests
{
    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ProductCategory.Create(Guid.NewGuid(), " "));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var category = ProductCategory.Create(Guid.NewGuid(), "Electronics");

        category.Deactivate();
        Assert.Equal(ProductCategoryStatus.Inactive, category.Status);

        category.Activate();
        Assert.Equal(ProductCategoryStatus.Active, category.Status);
    }
}
