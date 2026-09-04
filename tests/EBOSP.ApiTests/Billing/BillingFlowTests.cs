using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Billing;

namespace EBOSP.ApiTests.Billing;

/// <summary>Dev guide §15's Delivery -> Invoice -> Payment tail and §16's "never trust a client-provided paid flag" / idempotent payment guarantees.</summary>
public class BillingFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullFlow_InvoiceCreatePaymentConfirm_InvoiceBecomesPaid()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (customerId, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);

        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        Assert.Equal(total, invoice.Total);
        Assert.Equal(customerId, invoice.CustomerId);
        Assert.Equal("Issued", invoice.Status);

        var paymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "BankTransfer" });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        Assert.Equal("Pending", payment.Status);
        Assert.Null(payment.ConfirmedAt);

        var confirmResponse = await adminClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = (await confirmResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        Assert.Equal("Successful", confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAt);

        var invoiceAfter = await adminClient.GetAsync($"/api/v1/invoices/{invoice.Id}");
        Assert.Equal("Paid", (await invoiceAfter.Content.ReadFromJsonAsync<InvoiceResponse>())!.Status);
    }

    [Fact]
    public async Task CreatePayment_NeverStartsAnythingOtherThanPending()
    {
        // Dev guide §16: "never trust a client-provided 'paid' flag" - CreatePaymentRequest has no
        // status field to send in the first place, and this asserts what actually persists.
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;

        var paymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        var reloaded = await adminClient.GetAsync($"/api/v1/payments/{payment.Id}");
        Assert.Equal("Pending", (await reloaded.Content.ReadFromJsonAsync<PaymentResponse>())!.Status);
    }

    [Fact]
    public async Task ConfirmPayment_ConcurrentSplitPayments_BothSucceedAndInvoiceReachesPaid()
    {
        // Regression test for a hardening-review finding: two Pending payments that together
        // exactly cover an invoice, confirmed at the same time, could each compute PaidTotal from a
        // stale snapshot and neither would ever mark the invoice Paid - a normal split-payment
        // scenario, not an adversarial one. Fixed with an xmin concurrency token on Invoice plus a
        // reload-and-retry loop (same shape as InventoryService's concurrency retry from M4).
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 10, unitPrice: 10m);
        Assert.Equal(100m, total);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;

        var paymentAResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = 60m, Method = "Cash" });
        var paymentA = (await paymentAResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        var paymentBResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = 40m, Method = "Cash" });
        var paymentB = (await paymentBResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        var tasks = new[] { paymentA.Id, paymentB.Id }.Select(id =>
        {
            var client = factory.CreateClient();
            AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
            return client.PostAsync($"/api/v1/payments/{id}/confirm", content: null);
        });
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var invoiceAfter = await adminClient.GetAsync($"/api/v1/invoices/{invoice.Id}");
        Assert.Equal("Paid", (await invoiceAfter.Content.ReadFromJsonAsync<InvoiceResponse>())!.Status);
    }

    [Fact]
    public async Task CreateInvoice_FromNonFulfilledOrder_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        (await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new EBOSP.Contracts.Inventory.ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new EBOSP.Contracts.Inventory.ReceiveStockLine { ProductId = productId, Quantity = 10 }],
        })).EnsureSuccessStatusCode();
        var customerId = await Sales.SalesTestHelpers.CreateCustomerAsync(adminClient);
        var quoteResponse = await adminClient.PostAsJsonAsync("/api/v1/quotations", new EBOSP.Contracts.Sales.CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new EBOSP.Contracts.Sales.CreateQuotationLine { ProductId = productId, Quantity = 10, UnitPrice = 5m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<EBOSP.Contracts.Sales.QuotationResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();
        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new EBOSP.Contracts.Sales.CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        var order = (await orderResponse.Content.ReadFromJsonAsync<EBOSP.Contracts.Sales.SalesOrderResponse>())!;
        // Deliberately not delivered - order is still Open, not Fulfilled.

        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = order.Id });

        Assert.Equal(HttpStatusCode.Conflict, invoiceResponse.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_DuplicateForSameOrder_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, _) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);

        var first = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ConfirmPayment_WouldExceedInvoiceTotal_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;

        var firstPaymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "Cash" });
        var firstPayment = (await firstPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        (await adminClient.PostAsync($"/api/v1/payments/{firstPayment.Id}/confirm", content: null)).EnsureSuccessStatusCode();

        // Invoice is now fully paid - a second payment against it should never be confirmable.
        var secondPaymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = 1m, Method = "Cash" });
        Assert.Equal(HttpStatusCode.Conflict, secondPaymentResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_DuplicateIdempotencyKey_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        var key = Guid.NewGuid().ToString();

        var request = new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "Cash", IdempotencyKey = key };
        var first = await adminClient.PostAsJsonAsync("/api/v1/payments", request);
        var second = await adminClient.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ConfirmPayment_AboveThreshold_RequiresPaymentCreateLarge()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        // Default threshold is 10000 - well above it.
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 200, unitPrice: 100m);
        Assert.Equal(20000m, total);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;

        var (financeClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "finance");
        var paymentResponse = await financeClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "BankTransfer" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        // finance holds payment.create but not payment.create.large.
        var deniedResponse = await financeClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        // The tenant-admin holds payment.create.large.
        var allowedResponse = await adminClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_AnotherTenants_Returns404()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(tenantAClient);
        var (_, orderId, _) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(tenantAClient, branchId, warehouseId, productId);
        var invoiceResponse = await tenantAClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);

        var response = await tenantBClient.GetAsync($"/api/v1/invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
