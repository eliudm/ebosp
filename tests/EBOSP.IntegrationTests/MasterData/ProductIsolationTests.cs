using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.MasterData;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter." SKU uniqueness is per-tenant (dev guide §12).</summary>
[Collection(DatabaseCollection.Name)]
public class ProductIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Products_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Tenant MD-A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Tenant MD-B " + Guid.NewGuid());
        var productA = Product.Create(tenantA.Id, null, $"SKU-{Guid.NewGuid():N}", "Product A", null, 10m, 16m, 5);
        var productB = Product.Create(tenantB.Id, null, $"SKU-{Guid.NewGuid():N}", "Product B", null, 10m, 16m, 5);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Products.AddRange(productA, productB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Products.Where(p => p.Id == productA.Id || p.Id == productB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(productA.Id, visible[0].Id);
    }

    [Fact]
    public async Task SameSku_DifferentTenants_BothAllowed_SameTenant_Rejected()
    {
        var tenantA = Tenant.Create("Tenant MD-C " + Guid.NewGuid());
        var tenantB = Tenant.Create("Tenant MD-D " + Guid.NewGuid());
        var sharedSku = $"SKU-{Guid.NewGuid():N}";

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.AddRange(tenantA, tenantB);
        context.Products.Add(Product.Create(tenantA.Id, null, sharedSku, "Product A", null, 10m, 16m, 5));
        context.Products.Add(Product.Create(tenantB.Id, null, sharedSku, "Product B", null, 10m, 16m, 5));
        await context.SaveChangesAsync();

        context.Products.Add(Product.Create(tenantA.Id, null, sharedSku, "Product A duplicate", null, 10m, 16m, 5));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
