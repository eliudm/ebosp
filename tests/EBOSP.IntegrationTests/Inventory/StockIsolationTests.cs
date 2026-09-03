using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Inventory;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to inventory tables as to master data.</summary>
[Collection(DatabaseCollection.Name)]
public class StockIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task StockBalances_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, warehouseA, productA) = await SeedTenantWithWarehouseAndProductAsync("A");
        var (tenantB, warehouseB, productB) = await SeedTenantWithWarehouseAndProductAsync("B");
        var balanceA = StockBalance.CreateEmpty(tenantA.Id, warehouseA.Id, productA.Id);
        var balanceB = StockBalance.CreateEmpty(tenantB.Id, warehouseB.Id, productB.Id);
        balanceA.Receive(50);
        balanceB.Receive(50);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.StockBalances.AddRange(balanceA, balanceB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.StockBalances.Where(b => b.Id == balanceA.Id || b.Id == balanceB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(balanceA.Id, visible[0].Id);
    }

    [Fact]
    public async Task StockLedgerEntries_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, warehouseA, productA) = await SeedTenantWithWarehouseAndProductAsync("C");
        var (tenantB, warehouseB, productB) = await SeedTenantWithWarehouseAndProductAsync("D");
        var entryA = StockLedgerEntry.Create(tenantA.Id, warehouseA.Id, productA.Id, StockLedgerEventType.Received, 10, Guid.NewGuid(), DateTimeOffset.UtcNow, null, null, null, null);
        var entryB = StockLedgerEntry.Create(tenantB.Id, warehouseB.Id, productB.Id, StockLedgerEventType.Received, 10, Guid.NewGuid(), DateTimeOffset.UtcNow, null, null, null, null);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.StockLedgerEntries.AddRange(entryA, entryB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.StockLedgerEntries.Where(e => e.Id == entryA.Id || e.Id == entryB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(entryA.Id, visible[0].Id);
    }

    private async Task<(Tenant Tenant, Warehouse Warehouse, Product Product)> SeedTenantWithWarehouseAndProductAsync(string label)
    {
        var tenant = Tenant.Create($"Inv Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, 0);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (tenant, warehouse, product);
    }
}
