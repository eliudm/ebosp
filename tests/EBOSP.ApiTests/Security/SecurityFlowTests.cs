using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Billing;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Security;

namespace EBOSP.ApiTests.Security;

/// <summary>Dev guide §18.1's anomaly pipeline and alert lifecycle (Open -> Acknowledged -> Investigating -> Resolved/FalsePositive).</summary>
public class SecurityFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task RepeatedLoginFailures_RaisesHighSeverityAlert()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        var targetEmail = $"lockout-{Guid.NewGuid():N}@example.com";
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = targetEmail, Password = "SomePassword123!" });
        var created = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;
        (await adminClient.PostAsync($"/api/v1/users/{created.Id}/activate", content: null)).EnsureSuccessStatusCode();

        var attackerClient = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            await attackerClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = targetEmail, Password = "WrongPassword!" });
        }

        var (securityClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "security-officer");
        var alertsResponse = await securityClient.GetAsync("/api/v1/security/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;

        var alert = Assert.Single(alerts.Items, a => a.Rule == "RepeatedLoginFailures");
        Assert.Equal("High", alert.Severity);
        Assert.Equal("Open", alert.Status);
    }

    [Fact]
    public async Task ConcurrentFailedLogins_StillLockOutAndRaiseAlert()
    {
        // Regression test for a hardening-review finding: User had no concurrency token, so
        // concurrent failed-login requests raced on a plain in-memory FailedLoginCount++ and could
        // lose updates - an attacker firing guesses concurrently instead of sequentially could keep
        // the count from ever reaching the lockout threshold, evading both lockout and this alert.
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        var targetEmail = $"lockout-concurrent-{Guid.NewGuid():N}@example.com";
        const string correctPassword = "SomePassword123!";
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = targetEmail, Password = correctPassword });
        var created = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;
        (await adminClient.PostAsync($"/api/v1/users/{created.Id}/activate", content: null)).EnsureSuccessStatusCode();

        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var client = factory.CreateClient();
            return client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = targetEmail, Password = "WrongPassword!" });
        });
        await Task.WhenAll(tasks);

        // If FailedLoginCount lost updates under concurrency, this would wrongly succeed instead of
        // being rejected for a locked-out account - the direct proof the count really reached 10.
        var correctPasswordClient = factory.CreateClient();
        var lockedOutResponse = await correctPasswordClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = targetEmail, Password = correctPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedOutResponse.StatusCode);

        var alertsResponse = await adminClient.GetAsync("/api/v1/security/alerts");
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;
        Assert.Contains(alerts.Items, a => a.Rule == "RepeatedLoginFailures");
    }

    [Fact]
    public async Task LargeStockAdjustment_RaisesCriticalSeverityAlert()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (_, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        (await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 1000 }],
        })).EnsureSuccessStatusCode();

        // Default threshold is 100.
        var adjustResponse = await adminClient.PostAsJsonAsync("/api/v1/inventory/adjustments", new AdjustStockRequest
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityDelta = -150,
            Reason = "Damaged stock write-off",
        });
        Assert.Equal(HttpStatusCode.Created, adjustResponse.StatusCode);

        var alertsResponse = await adminClient.GetAsync("/api/v1/security/alerts");
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;
        var alert = Assert.Single(alerts.Items, a => a.Rule == "LargeStockAdjustment");
        Assert.Equal("Critical", alert.Severity);
    }

    [Fact]
    public async Task UnusualPayment_RaisesCriticalSeverityAlert()
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
        var paymentResponse = await adminClient.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest { InvoiceId = invoice.Id, Amount = total, Method = "BankTransfer" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        (await adminClient.PostAsync($"/api/v1/payments/{payment.Id}/confirm", content: null)).EnsureSuccessStatusCode();

        var alertsResponse = await adminClient.GetAsync("/api/v1/security/alerts");
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;
        var alert = Assert.Single(alerts.Items, a => a.Rule == "UnusualPayment");
        Assert.Equal("Critical", alert.Severity);
    }

    [Fact]
    public async Task FullLifecycle_AcknowledgeInvestigateResolve()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var alert = await RaiseLockoutAlertAsync(adminClient, factory, admin);

        var ackResponse = await adminClient.PostAsync($"/api/v1/security/alerts/{alert.Id}/acknowledge", content: null);
        Assert.Equal(HttpStatusCode.OK, ackResponse.StatusCode);
        Assert.Equal("Acknowledged", (await ackResponse.Content.ReadFromJsonAsync<SecurityAlertResponse>())!.Status);

        var investigateResponse = await adminClient.PostAsync($"/api/v1/security/alerts/{alert.Id}/investigate", content: null);
        Assert.Equal(HttpStatusCode.OK, investigateResponse.StatusCode);
        Assert.Equal("Investigating", (await investigateResponse.Content.ReadFromJsonAsync<SecurityAlertResponse>())!.Status);

        var resolveResponse = await adminClient.PostAsJsonAsync($"/api/v1/security/alerts/{alert.Id}/resolve", new ResolveSecurityAlertRequest { Notes = "Confirmed with the user - forgotten password." });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolved = (await resolveResponse.Content.ReadFromJsonAsync<SecurityAlertResponse>())!;
        Assert.Equal("Resolved", resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public async Task MarkFalsePositive_FromOpen_Succeeds()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var alert = await RaiseLockoutAlertAsync(adminClient, factory, admin);

        var response = await adminClient.PostAsJsonAsync($"/api/v1/security/alerts/{alert.Id}/false-positive", new ResolveSecurityAlertRequest { Notes = "Known QA automation account." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("FalsePositive", (await response.Content.ReadFromJsonAsync<SecurityAlertResponse>())!.Status);
    }

    [Fact]
    public async Task Investigate_StillOpen_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var alert = await RaiseLockoutAlertAsync(adminClient, factory, admin);

        var response = await adminClient.PostAsync($"/api/v1/security/alerts/{alert.Id}/investigate", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task NonSecurityOfficer_Denied_Returns403()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        var response = await salesClient.GetAsync("/api/v1/security/alerts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSecurityAlert_AnotherTenants_Returns404()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var alert = await RaiseLockoutAlertAsync(tenantAClient, factory, tenantA);

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);

        var response = await tenantBClient.GetAsync($"/api/v1/security/alerts/{alert.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<SecurityAlertResponse> RaiseLockoutAlertAsync(HttpClient adminClient, CustomWebApplicationFactory factory, TenantAdminContext admin)
    {
        var targetEmail = $"lockout-{Guid.NewGuid():N}@example.com";
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = targetEmail, Password = "SomePassword123!" });
        var created = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;
        (await adminClient.PostAsync($"/api/v1/users/{created.Id}/activate", content: null)).EnsureSuccessStatusCode();

        var attackerClient = factory.CreateClient();
        for (var i = 0; i < 10; i++)
        {
            await attackerClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = targetEmail, Password = "WrongPassword!" });
        }

        var alertsResponse = await adminClient.GetAsync("/api/v1/security/alerts");
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;
        return alerts.Items.Single(a => a.Rule == "RepeatedLoginFailures");
    }
}
