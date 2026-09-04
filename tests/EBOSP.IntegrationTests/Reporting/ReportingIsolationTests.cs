using EBOSP.Application.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Reporting;

/// <summary>
/// Spec §12/§19: an aggregation leak here wouldn't 404 the way a cross-tenant IDOR normally
/// does - it would silently blend another tenant's numbers into the total. Seeds two tenants with
/// overlapping data in the same date/status window and asserts the aggregate for one never
/// reflects the other's rows.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ReportingIsolationTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task SalesReport_AggregatesOnlyTheRequestedTenantsData()
    {
        var (tenantA, orderA, invoiceA) = await SeedBillingDataAsync("A", orderTotal: 100m, paymentAmount: 60m);
        var (tenantB, orderB, invoiceB) = await SeedBillingDataAsync("B", orderTotal: 900m, paymentAmount: 900m);

        // The global tenant query filter is keyed off the ambient ICurrentUserContext, exactly as
        // it is for every real request - set it to tenant A, same as the API always does, so this
        // proves the repository's own explicit filter isn't the only thing standing between tenant
        // B's rows and tenant A's totals.
        var repository = new ReportingRepository(fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id }), new FixedClock(Now));
        var report = await repository.GetSalesReportAsync(tenantA.Id, Now.AddDays(-1), Now.AddDays(1), CancellationToken.None);

        Assert.Equal(1, report.TotalOrders);
        Assert.Equal(orderA.Total, report.TotalOrderValue);
        Assert.Equal(1, report.TotalInvoices);
        Assert.Equal(invoiceA.Total, report.TotalInvoiced);
        Assert.Equal(60m, report.TotalCollected);
        // If tenant B's much larger order/invoice/payment ever leaked in, these would be 1000/60 vs
        // the true 100/60 - proving the aggregate, not just row visibility, stays tenant-scoped.
        Assert.NotEqual(orderA.Total + orderB.Total, report.TotalOrderValue);
    }

    [Fact]
    public async Task LowStockReport_ExcludesOtherTenantsBalances()
    {
        var (tenantA, warehouseA, productA) = await SeedLowStockBalanceAsync("C", quantityOnHand: 2, reorderLevel: 10);
        var (tenantB, warehouseB, productB) = await SeedLowStockBalanceAsync("D", quantityOnHand: 1, reorderLevel: 5);

        var repository = new ReportingRepository(fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id }), new FixedClock(Now));
        var result = await repository.GetLowStockAsync(tenantA.Id, new EBOSP.Contracts.Common.PagedRequest(), CancellationToken.None);

        var item = Assert.Single(result.Items, i => i.WarehouseId == warehouseA.Id || i.WarehouseId == warehouseB.Id);
        Assert.Equal(warehouseA.Id, item.WarehouseId);
        Assert.Equal(productA.Id, item.ProductId);
    }

    private async Task<(Tenant Tenant, SalesOrder Order, Invoice Invoice)> SeedBillingDataAsync(string label, decimal orderTotal, decimal paymentAmount)
    {
        var tenant = Tenant.Create($"Reporting Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var customer = Customer.Create(tenant.Id, "Customer " + label, 100000m);
        var unitPrice = orderTotal / 10;
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, unitPrice, 0m, 0);
        var quotation = Quotation.Create(tenant.Id, customer.Id, branch.Id, [(product.Id, 10, unitPrice)]);
        quotation.Accept();
        var reservation = StockReservation.Create(tenant.Id, warehouse.Id, product.Id, 10, Now);
        var order = SalesOrder.Create(tenant.Id, quotation.Id, customer.Id, branch.Id, warehouse.Id, Guid.NewGuid(), Now, [(product.Id, 10, unitPrice, reservation.Id)]);
        order.MarkFulfilled();
        var invoice = Invoice.Create(tenant.Id, order.Id, customer.Id, order.Total, Guid.NewGuid(), Now);
        var payment = Payment.Create(tenant.Id, invoice.Id, paymentAmount, "Cash", Guid.NewGuid(), Now, null);
        payment.Confirm(Guid.NewGuid(), Now);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Customers.Add(customer);
        context.Products.Add(product);
        context.Quotations.Add(quotation);
        context.StockReservations.Add(reservation);
        context.SalesOrders.Add(order);
        context.Invoices.Add(invoice);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        return (tenant, order, invoice);
    }

    private async Task<(Tenant Tenant, Warehouse Warehouse, Product Product)> SeedLowStockBalanceAsync(string label, int quantityOnHand, int reorderLevel)
    {
        var tenant = Tenant.Create($"Reporting Tenant {label} " + Guid.NewGuid());
        var branch = Branch.Create(tenant.Id, "HQ");
        var warehouse = Warehouse.Create(tenant.Id, branch.Id, "Main");
        var product = Product.Create(tenant.Id, null, $"SKU-{Guid.NewGuid():N}", "Widget", null, 1m, 0m, reorderLevel);
        var balance = StockBalance.CreateEmpty(tenant.Id, warehouse.Id, product.Id);
        balance.Receive(quantityOnHand);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.Add(tenant);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.StockBalances.Add(balance);
        await context.SaveChangesAsync();

        return (tenant, warehouse, product);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
