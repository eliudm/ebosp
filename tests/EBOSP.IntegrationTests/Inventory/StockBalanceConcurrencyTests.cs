using EBOSP.Application.Common;
using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Inventory;

/// <summary>
/// Proves the xmin-based optimistic concurrency mapping (AppDbContext) actually works at the
/// database level - two separate DbContext instances loading the same row (simulating two
/// concurrent requests) racing to save a conflicting change (dev guide §13.4: "concurrent issue
/// operations cannot corrupt balance").
/// </summary>
[Collection(DatabaseCollection.Name)]
public class StockBalanceConcurrencyTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task ConcurrentUpdate_ToSameBalance_SecondSaveThrowsConcurrencyConflict()
    {
        var (tenant, balance) = await SeedBalanceAsync(100);
        var userContext = new TestCurrentUserContext { TenantId = tenant.Id };

        await using var contextA = fixture.CreateContext(userContext);
        await using var contextB = fixture.CreateContext(userContext);

        var balanceA = await contextA.StockBalances.SingleAsync(b => b.Id == balance.Id);
        var balanceB = await contextB.StockBalances.SingleAsync(b => b.Id == balance.Id);

        balanceA.Issue(10);
        balanceB.Issue(20);

        await contextA.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => contextB.SaveChangesAsync());
    }

    [Fact]
    public async Task Reload_AfterConflict_ThenReapply_ProducesCorrectFinalBalance()
    {
        var (tenant, balance) = await SeedBalanceAsync(100);
        var userContext = new TestCurrentUserContext { TenantId = tenant.Id };

        await using var contextA = fixture.CreateContext(userContext);
        await using var contextB = fixture.CreateContext(userContext);

        var balanceA = await contextA.StockBalances.SingleAsync(b => b.Id == balance.Id);
        var balanceB = await contextB.StockBalances.SingleAsync(b => b.Id == balance.Id);

        balanceA.Issue(10);
        balanceB.Issue(20);

        await contextA.SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => contextB.SaveChangesAsync());

        // This is exactly what InventoryService's retry loop does: reload, then reapply the same
        // relative mutation against the now-current state.
        await contextB.Entry(balanceB).ReloadAsync();
        balanceB.Issue(20);
        await contextB.SaveChangesAsync();

        await using var verifyContext = fixture.CreateContext(userContext);
        var final = await verifyContext.StockBalances.SingleAsync(b => b.Id == balance.Id);
        Assert.Equal(70, final.QuantityOnHand);
    }

    private async Task<(Tenant Tenant, StockBalance Balance)> SeedBalanceAsync(int initialQuantity)
    {
        var tenant = Tenant.Create("Inv Concurrency Tenant " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, 0);
        var balance = StockBalance.CreateEmpty(tenant.Id, warehouse.Id, product.Id);
        balance.Receive(initialQuantity);

        await using var seedContext = fixture.CreateContext(new TestCurrentUserContext());
        seedContext.Tenants.Add(tenant);
        seedContext.Branches.Add(branch);
        seedContext.Warehouses.Add(warehouse);
        seedContext.Products.Add(product);
        seedContext.StockBalances.Add(balance);
        await seedContext.SaveChangesAsync();

        return (tenant, balance);
    }
}
