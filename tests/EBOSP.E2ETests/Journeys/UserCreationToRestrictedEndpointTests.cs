using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Identity;
using EBOSP.Contracts.Procurement;

namespace EBOSP.E2ETests.Journeys;

/// <summary>
/// Dev guide §25.3 "User creation -> role assignment -> restricted endpoint". The per-module
/// ApiTests suite proves create+assign+activate+login works (AuthFlowTests) and proves a
/// generic authenticated-but-unpermitted request gets 403 (AuthorizationMatrixTests/
/// AuthorizationCoverageTests), but nothing chains a *freshly* role-assigned user straight into a
/// permission-gated call to prove that specific assignment actually took effect - that's the leg
/// this journey closes, both positively (the granted permission works) and negatively (a
/// permission NOT granted by that role is still refused).
/// </summary>
public class UserCreationToRestrictedEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullJourney_CreateUserAssignRoleActivate_GrantedPermissionWorksAndUngrantedOneDoesNot()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        var email = $"e2e-role-{Guid.NewGuid():N}@example.com";
        const string password = "SomePassword123!";
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var user = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;

        // "procurement" grants procurement.create but not master-data.manage.
        Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PostAsJsonAsync($"/api/v1/users/{user.Id}/roles", new AssignRoleRequest { RoleCode = "procurement" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PostAsync($"/api/v1/users/{user.Id}/activate", content: null)).StatusCode);

        var newUserClient = factory.CreateClient();
        var tokens = await AuthTestHelpers.LoginAsync(newUserClient, email, password);
        AuthTestHelpers.AuthorizeAs(newUserClient, tokens);

        var grantedResponse = await newUserClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "E2E journey - role actually works",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        Assert.Equal(HttpStatusCode.Created, grantedResponse.StatusCode);

        var ungrantedResponse = await newUserClient.PostAsJsonAsync("/api/v1/branches", new EBOSP.Contracts.MasterData.CreateBranchRequest { Name = "Should not be creatable " + Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Forbidden, ungrantedResponse.StatusCode);
    }
}
