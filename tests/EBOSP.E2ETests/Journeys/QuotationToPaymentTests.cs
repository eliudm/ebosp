using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests;
using EBOSP.ApiTests.Billing;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Billing;

namespace EBOSP.E2ETests.Journeys;

/// <summary>
/// Dev guide §25.3 "Quotation -> order -> delivery -> invoice -> payment". Genuinely chained -
/// BillingTestHelpers.CreateFulfilledSalesOrderAsync already walks quotation-accept-order-deliver
/// as one sequence; this test continues from its actual returned order id into invoice and payment,
/// rather than starting invoice/payment from fresh, unrelated seed data the way the per-module
/// ApiTests flow tests do for isolation.
/// </summary>
public class QuotationToPaymentTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullJourney_QuoteOrderDeliverInvoicePay_InvoiceReachesPaid()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId, quantity: 8, unitPrice: 12.5m);
        Assert.Equal(100m, total);

        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        Assert.Equal(total, invoice.Total);

        var paymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "BankTransfer" });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        var confirmResponse = await adminClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var invoiceAfter = await adminClient.GetAsync($"/api/v1/invoices/{invoice.Id}");
        var finalInvoice = (await invoiceAfter.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        Assert.Equal("Paid", finalInvoice.Status);
    }
}
