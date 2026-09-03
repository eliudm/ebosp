using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.MasterData;

public class ProductTests
{
    private static Product NewProduct() =>
        Product.Create(Guid.NewGuid(), categoryId: null, sku: "SKU-1", name: "Widget", description: null, unitPrice: 9.99m, taxRatePercent: 16m, reorderLevel: 5);

    [Fact]
    public void Create_NegativeUnitPrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create(Guid.NewGuid(), null, "SKU-1", "Widget", null, -1m, 16m, 5));
    }

    [Fact]
    public void Create_NegativeReorderLevel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create(Guid.NewGuid(), null, "SKU-1", "Widget", null, 9.99m, 16m, -1));
    }

    [Fact]
    public void Create_BlankSku_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create(Guid.NewGuid(), null, "  ", "Widget", null, 9.99m, 16m, 5));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var product = NewProduct();

        product.Deactivate();
        Assert.Equal(ProductStatus.Inactive, product.Status);

        product.Activate();
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Update_AppliesNewValues()
    {
        var product = NewProduct();
        var categoryId = Guid.NewGuid();

        product.Update(categoryId, "New Name", "New description", 19.99m, 8m, 10);

        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal("New Name", product.Name);
        Assert.Equal("New description", product.Description);
        Assert.Equal(19.99m, product.UnitPrice);
        Assert.Equal(8m, product.TaxRatePercent);
        Assert.Equal(10, product.ReorderLevel);
    }
}
