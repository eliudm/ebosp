using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Sales;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to sales tables as to procurement/inventory/master data.</summary>
[Collection(DatabaseCollection.Name)]
public class SalesIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Customers_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Sales Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Sales Tenant B " + Guid.NewGuid());
        var customerA = Customer.Create(tenantA.Id, "Acme Corp", 1000m);
        var customerB = Customer.Create(tenantB.Id, "Acme Corp", 1000m);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Customers.AddRange(customerA, customerB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Customers.Where(c => c.Id == customerA.Id || c.Id == customerB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(customerA.Id, visible[0].Id);
    }

    [Fact]
    public async Task SalesOrders_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, branchA, warehouseA, customerA, productA) = await SeedTenantWithSalesPrereqsAsync("C");
        var (tenantB, branchB, warehouseB, customerB, productB) = await SeedTenantWithSalesPrereqsAsync("D");
        var quotationA = Quotation.Create(tenantA.Id, customerA.Id, branchA.Id, [(productA.Id, 5, 10m)]);
        var quotationB = Quotation.Create(tenantB.Id, customerB.Id, branchB.Id, [(productB.Id, 5, 10m)]);
        quotationA.Accept();
        quotationB.Accept();
        var reservationA = StockReservation.Create(tenantA.Id, warehouseA.Id, productA.Id, 5, DateTimeOffset.UtcNow);
        var reservationB = StockReservation.Create(tenantB.Id, warehouseB.Id, productB.Id, 5, DateTimeOffset.UtcNow);
        var orderA = SalesOrder.Create(tenantA.Id, quotationA.Id, customerA.Id, branchA.Id, warehouseA.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, [(productA.Id, 5, 10m, reservationA.Id)]);
        var orderB = SalesOrder.Create(tenantB.Id, quotationB.Id, customerB.Id, branchB.Id, warehouseB.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, [(productB.Id, 5, 10m, reservationB.Id)]);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Quotations.AddRange(quotationA, quotationB);
            seedContext.StockReservations.AddRange(reservationA, reservationB);
            seedContext.SalesOrders.AddRange(orderA, orderB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.SalesOrders.Where(o => o.Id == orderA.Id || o.Id == orderB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(orderA.Id, visible[0].Id);
    }

    private async Task<(Tenant Tenant, Branch Branch, Warehouse Warehouse, Customer Customer, Product Product)> SeedTenantWithSalesPrereqsAsync(string label)
    {
        var tenant = Tenant.Create($"Sales Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var customer = Customer.Create(tenant.Id, "Customer " + label, 100000m);
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, 0);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (tenant, branch, warehouse, customer, product);
    }
}
