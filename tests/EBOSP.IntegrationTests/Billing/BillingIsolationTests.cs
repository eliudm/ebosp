using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Billing;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to billing tables as to sales/procurement/inventory/master data.</summary>
[Collection(DatabaseCollection.Name)]
public class BillingIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Invoices_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, orderA) = await SeedTenantWithFulfilledOrderAsync("E");
        var (tenantB, orderB) = await SeedTenantWithFulfilledOrderAsync("F");
        var invoiceA = Invoice.Create(tenantA.Id, orderA.Id, orderA.CustomerId, orderA.Total, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var invoiceB = Invoice.Create(tenantB.Id, orderB.Id, orderB.CustomerId, orderB.Total, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Invoices.AddRange(invoiceA, invoiceB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Invoices.Where(i => i.Id == invoiceA.Id || i.Id == invoiceB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(invoiceA.Id, visible[0].Id);
    }

    [Fact]
    public async Task Payments_QueryFilter_ExcludesOtherTenants()
    {
        var (tenantA, orderA) = await SeedTenantWithFulfilledOrderAsync("G");
        var (tenantB, orderB) = await SeedTenantWithFulfilledOrderAsync("H");
        var invoiceA = Invoice.Create(tenantA.Id, orderA.Id, orderA.CustomerId, orderA.Total, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var invoiceB = Invoice.Create(tenantB.Id, orderB.Id, orderB.CustomerId, orderB.Total, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var paymentA = Payment.Create(tenantA.Id, invoiceA.Id, invoiceA.Total, "Cash", Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var paymentB = Payment.Create(tenantB.Id, invoiceB.Id, invoiceB.Total, "Cash", Guid.NewGuid(), DateTimeOffset.UtcNow, null);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Invoices.AddRange(invoiceA, invoiceB);
            seedContext.Payments.AddRange(paymentA, paymentB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Payments.Where(p => p.Id == paymentA.Id || p.Id == paymentB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(paymentA.Id, visible[0].Id);
    }

    private async Task<(Tenant Tenant, SalesOrder Order)> SeedTenantWithFulfilledOrderAsync(string label)
    {
        var tenant = Tenant.Create($"Billing Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var customer = Customer.Create(tenant.Id, "Customer " + label, 100000m);
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, 0);
        var quotation = Quotation.Create(tenant.Id, customer.Id, branch.Id, [(product.Id, 5, 10m)]);
        quotation.Accept();
        var reservation = StockReservation.Create(tenant.Id, warehouse.Id, product.Id, 5, DateTimeOffset.UtcNow);
        var order = SalesOrder.Create(tenant.Id, quotation.Id, customer.Id, branch.Id, warehouse.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, [(product.Id, 5, 10m, reservation.Id)]);
        order.MarkFulfilled();

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Customers.Add(customer);
        context.Products.Add(product);
        context.Quotations.Add(quotation);
        context.StockReservations.Add(reservation);
        context.SalesOrders.Add(order);
        await context.SaveChangesAsync();

        return (tenant, order);
    }
}
