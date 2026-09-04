using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Audit;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;

namespace EBOSP.ApiTests.Audit;

/// <summary>Dev guide §18's audit search - reads themselves need "audit.read", not just writes.</summary>
public class AuditFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ListAuditEvents_TenantAdmin_SeesRecordedEvents()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var branchResponse = await adminClient.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "HQ " + Guid.NewGuid() });
        branchResponse.EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/audit-events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<AuditEventResponse>>())!;
        Assert.Contains(page.Items, e => e.EventType == "BranchCreated");
    }

    [Fact]
    public async Task ListAuditEvents_FilteredByEventType_ReturnsOnlyMatching()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        (await adminClient.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "HQ " + Guid.NewGuid() })).EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/audit-events?eventType=BranchCreated");

        var page = (await response.Content.ReadFromJsonAsync<PagedResult<AuditEventResponse>>())!;
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, e => Assert.Equal("BranchCreated", e.EventType));
    }

    [Fact]
    public async Task NonAuditor_Denied_Returns403()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        var response = await salesClient.GetAsync("/api/v1/audit-events");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListAuditEvents_ExcludesOtherTenantsEvents()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var branchName = "Unique Branch " + Guid.NewGuid();
        (await tenantAClient.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = branchName })).EnsureSuccessStatusCode();

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);

        var response = await tenantBClient.GetAsync("/api/v1/audit-events?eventType=BranchCreated");
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<AuditEventResponse>>())!;

        Assert.DoesNotContain(page.Items, e => e.Payload.Contains(branchName));
    }
}
