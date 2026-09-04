using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Billing;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.MasterData;
using EBOSP.Contracts.Procurement;
using EBOSP.Contracts.Reporting;

namespace EBOSP.ApiTests.Reporting;

/// <summary>Spec §8.6/§19 read-only report endpoints - [Authorize] only, no new permission (see ReportsController).</summary>
public class ReportsFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SalesReport_TotalsMatchSeededOrderInvoiceAndPayment()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 10, unitPrice: 5m);
        Assert.Equal(50m, total);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        var paymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = 30m, Method = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        (await adminClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null)).EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/reports/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<SalesReportResponse>())!;
        Assert.Equal(1, report.TotalOrders);
        Assert.Equal(50m, report.TotalOrderValue);
        Assert.Equal(1, report.TotalInvoices);
        Assert.Equal(50m, report.TotalInvoiced);
        Assert.Equal(30m, report.TotalCollected);
        Assert.Equal(20m, report.TotalOutstanding);
    }

    [Fact]
    public async Task LowStockReport_IncludesBalanceBelowLevelAndExcludesBalanceAboveLevel()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, _) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var lowProductId = await CreateProductWithReorderLevelAsync(adminClient, reorderLevel: 10);
        var healthyProductId = await CreateProductWithReorderLevelAsync(adminClient, reorderLevel: 10);

        (await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = lowProductId, Quantity = 2 }, new ReceiveStockLine { ProductId = healthyProductId, Quantity = 50 }],
        })).EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/reports/low-stock?pageSize=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PagedResult<LowStockReportItem>>())!;
        Assert.Contains(result.Items, i => i.ProductId == lowProductId && i.QuantityOnHand == 2 && i.ReorderLevel == 10);
        Assert.DoesNotContain(result.Items, i => i.ProductId == healthyProductId);
    }

    [Fact]
    public async Task SlowMovingInventoryReport_ExcludesBalanceWithRecentMovement()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (_, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        (await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 5 }],
        })).EnsureSuccessStatusCode();

        // Just received - moved within the last second, so even the shortest sensible window
        // (1 day) must exclude it.
        var response = await adminClient.GetAsync("/api/v1/reports/slow-moving-inventory?daysInactive=1&pageSize=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PagedResult<SlowMovingInventoryItem>>())!;
        Assert.DoesNotContain(result.Items, i => i.ProductId == productId && i.WarehouseId == warehouseId);
    }

    [Fact]
    public async Task SlowMovingInventoryReport_ExtremeDaysInactive_IsClampedNotUnhandled()
    {
        // Regression test for a hardening-review finding: an unclamped daysInactive overflowed
        // DateTimeOffset.AddDays and threw an unhandled 500 instead of a bounded result.
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        var response = await adminClient.GetAsync($"/api/v1/reports/slow-moving-inventory?daysInactive={int.MaxValue}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcurementSpendReport_SumsCorrectlyPerSupplier()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(adminClient);
        await CreatePurchaseOrderAsync(adminClient, factory, branchId, productId, supplierId, quantity: 4, unitPrice: 25m); // 100
        await CreatePurchaseOrderAsync(adminClient, factory, branchId, productId, supplierId, quantity: 2, unitPrice: 25m); // 50

        var response = await adminClient.GetAsync("/api/v1/reports/procurement-spend");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<ProcurementSpendReportResponse>())!;
        Assert.Equal(2, report.TotalPurchaseOrders);
        Assert.Equal(150m, report.TotalSpend);
        var bySupplier = Assert.Single(report.BySupplier);
        Assert.Equal(supplierId, bySupplier.SupplierId);
        Assert.Equal(150m, bySupplier.Total);
    }

    [Fact]
    public async Task OutstandingInvoicesReport_ExcludesPaidAndIncludesPartiallyPaidWithCorrectBalance()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        var (_, paidOrderId, paidTotal) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 5, unitPrice: 10m);
        var paidInvoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = paidOrderId });
        var paidInvoice = (await paidInvoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        var paidPaymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = paidInvoice.Id, Amount = paidTotal, Method = "Cash" });
        var paidPayment = (await paidPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        (await adminClient.PostAsync($"/api/v1/payments/{paidPayment.Id}/confirm", content: null)).EnsureSuccessStatusCode();

        var (_, partialOrderId, partialTotal) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 10, unitPrice: 10m);
        var partialInvoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = partialOrderId });
        var partialInvoice = (await partialInvoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        var partialPaymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = partialInvoice.Id, Amount = 40m, Method = "Cash" });
        var partialPayment = (await partialPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        (await adminClient.PostAsync($"/api/v1/payments/{partialPayment.Id}/confirm", content: null)).EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/reports/outstanding-invoices?pageSize=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PagedResult<OutstandingInvoiceItem>>())!;
        Assert.DoesNotContain(result.Items, i => i.InvoiceId == paidInvoice.Id);
        var partial = Assert.Single(result.Items, i => i.InvoiceId == partialInvoice.Id);
        Assert.Equal(partialTotal, partial.Total);
        Assert.Equal(40m, partial.PaidTotal);
        Assert.Equal(partialTotal - 40m, partial.Outstanding);
    }

    [Fact]
    public async Task Reports_WithoutAuthentication_Returns401()
    {
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/v1/reports/sales");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Guid> CreateProductWithReorderLevelAsync(HttpClient client, int reorderLevel)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Product " + Guid.NewGuid(),
            UnitPrice = 10m,
            TaxRatePercent = 16m,
            ReorderLevel = reorderLevel,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }

    private static async Task CreatePurchaseOrderAsync(HttpClient approverClient, CustomWebApplicationFactory factory, Guid branchId, Guid productId, Guid supplierId, int quantity, decimal unitPrice)
    {
        // A separate requester, distinct from the approving `approverClient` - approving one's own
        // request is denied (PurchaseRequestService's self-approval guard).
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(approverClient, factory, "procurement");
        var requestResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = quantity, EstimatedUnitPrice = unitPrice }],
        });
        var request = (await requestResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        (await approverClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();

        (await approverClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice }],
        })).EnsureSuccessStatusCode();
    }
}
