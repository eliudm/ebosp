using System.Net;
using System.Net.Http.Json;
using System.Text;
using EBOSP.ApiTests.Billing;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.MasterData;

namespace EBOSP.ApiTests.Security;

/// <summary>
/// Dev guide §25.4's negative/security categories not already covered by an isolation, auth, rate-
/// limit or idempotency test elsewhere in this suite: SQL-injection-shaped input, an XSS-payload
/// string, mass assignment via extra JSON fields a DTO doesn't declare, and an oversized payload.
/// Everything here goes through EF Core (parameterized, never string-built SQL) and System.Text.Json
/// model binding (which only ever populates declared DTO properties) - these tests turn that into a
/// proven invariant instead of an assumption.
/// </summary>
public class MalformedPayloadTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SqlInjectionShapedName_StoredAndReturnedAsInertLiteralText()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        const string payload = "Widget'); DROP TABLE products; --";

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = payload,
            UnitPrice = 1m,
            TaxRatePercent = 0m,
            ReorderLevel = 0,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var product = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(payload, product.Name);

        // The table (and every other product in it) must still exist - EF Core parameterized the
        // value rather than concatenating it into a statement.
        var listResponse = await adminClient.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task XssPayloadName_StoredAndReturnedAsLiteralJsonText()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        const string payload = "<script>alert(document.cookie)</script>";

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = payload,
            UnitPrice = 1m,
            TaxRatePercent = 0m,
            ReorderLevel = 0,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var product = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        // This API is JSON-only and never server-side-renders a value into HTML, so there is no
        // stored-XSS surface here to begin with - this asserts exactly that: the payload round-trips
        // as inert text, never interpreted or stripped.
        Assert.Equal(payload, product.Name);
    }

    [Fact]
    public async Task MassAssignment_UndeclaredFieldsInPaymentCreateBody_AreIgnored()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (_, orderId, total) = await BillingTestHelpers.CreateFulfilledSalesOrderAsync(adminClient, branchId, warehouseId, productId);
        var invoiceResponse = await adminClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest { SalesOrderId = orderId });
        var invoice = (await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResponse>())!;
        var attackerChosenId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        // CreatePaymentRequest has no Status/Id/TenantId property - these are attempts to smuggle
        // them in anyway via the raw JSON body, bypassing the strongly-typed request DTO entirely.
        var rawJson = $$"""
            {
              "invoiceId": "{{invoice.Id}}",
              "amount": {{total}},
              "method": "Cash",
              "status": "Successful",
              "id": "{{attackerChosenId}}",
              "tenantId": "{{otherTenantId}}",
              "confirmedAt": "2020-01-01T00:00:00Z"
            }
            """;

        var response = await adminClient.PostAsync("/api/v1/payments", new StringContent(rawJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = (await response.Content.ReadFromJsonAsync<PaymentResponse>())!;
        Assert.NotEqual(attackerChosenId, payment.Id);
        Assert.Equal("Pending", payment.Status);
        Assert.Null(payment.ConfirmedAt);

        // The payment is still scoped to the caller's own tenant, not the smuggled tenantId - a
        // request for it under this same (correctly-scoped) client must find it.
        var getResponse = await adminClient.GetAsync($"/api/v1/payments/{payment.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task OversizedField_ExceedsMaxLength_Returns400NotServerError()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        // CreateProductRequest.Name is [MaxLength(200)] - this is 5000 characters.
        var oversizedName = new string('a', 5000);

        var response = await adminClient.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = oversizedName,
            UnitPrice = 1m,
            TaxRatePercent = 0m,
            ReorderLevel = 0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
