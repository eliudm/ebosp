using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Procurement;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to procurement tables as to master data/inventory.</summary>
[Collection(DatabaseCollection.Name)]
public class ProcurementIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Suppliers_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Proc Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Proc Tenant B " + Guid.NewGuid());
        var supplierA = Supplier.Create(tenantA.Id, "Acme Supplies", null, null);
        var supplierB = Supplier.Create(tenantB.Id, "Acme Supplies", null, null);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Suppliers.AddRange(supplierA, supplierB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Suppliers.Where(s => s.Id == supplierA.Id || s.Id == supplierB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(supplierA.Id, visible[0].Id);
    }

    [Fact]
    public async Task PurchaseRequests_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, branchA, productA) = await SeedTenantWithBranchAndProductAsync("C");
        var (tenantB, branchB, productB) = await SeedTenantWithBranchAndProductAsync("D");
        var requestA = PurchaseRequest.Create(tenantA.Id, Guid.NewGuid(), branchA.Id, "Restock A", DateTimeOffset.UtcNow, [(productA.Id, 5, 10m)]);
        var requestB = PurchaseRequest.Create(tenantB.Id, Guid.NewGuid(), branchB.Id, "Restock B", DateTimeOffset.UtcNow, [(productB.Id, 5, 10m)]);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.PurchaseRequests.AddRange(requestA, requestB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.PurchaseRequests.Where(r => r.Id == requestA.Id || r.Id == requestB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(requestA.Id, visible[0].Id);
    }

    private async Task<(Tenant Tenant, Branch Branch, Product Product)> SeedTenantWithBranchAndProductAsync(string label)
    {
        var tenant = Tenant.Create($"Proc Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, 0);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (tenant, branch, product);
    }
}
