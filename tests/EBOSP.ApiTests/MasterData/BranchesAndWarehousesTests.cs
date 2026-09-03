using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Contracts.MasterData;

namespace EBOSP.ApiTests.MasterData;

public class BranchesAndWarehousesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanCreateAndListBranches()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var createResponse = await client.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Nairobi HQ" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<BranchResponse>())!;
        Assert.Equal("Nairobi HQ", created.Name);

        var listResponse = await client.GetAsync("/api/v1/branches");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = (await listResponse.Content.ReadFromJsonAsync<PagedResult<BranchResponse>>())!;
        Assert.Contains(page.Items, b => b.Id == created.Id);
    }

    [Fact]
    public async Task UserWithoutPermission_CanRead_ButCannotWrite()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var noRoleEmail = $"norole-{Guid.NewGuid():N}@example.com";
        var createUserResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = noRoleEmail, Password = "SomePassword123!" });
        createUserResponse.EnsureSuccessStatusCode();

        var noRoleClient = factory.CreateClient();
        var noRoleTokens = await AuthTestHelpers.LoginAsync(noRoleClient, noRoleEmail, "SomePassword123!");
        AuthTestHelpers.AuthorizeAs(noRoleClient, noRoleTokens);

        var readResponse = await noRoleClient.GetAsync("/api/v1/branches");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var writeResponse = await noRoleClient.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Should Fail" });
        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/branches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateWarehouse_WithOwnTenantBranch_Succeeds()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var branchResponse = await client.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Mombasa" });
        var branch = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!;

        var warehouseResponse = await client.PostAsJsonAsync("/api/v1/warehouses", new CreateWarehouseRequest { BranchId = branch.Id, Name = "Mombasa Store" });
        Assert.Equal(HttpStatusCode.Created, warehouseResponse.StatusCode);
        var warehouse = (await warehouseResponse.Content.ReadFromJsonAsync<WarehouseResponse>())!;
        Assert.Equal(branch.Id, warehouse.BranchId);
    }

    [Fact]
    public async Task CreateWarehouse_WithAnotherTenantsBranch_Returns404()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var otherClient = factory.CreateClient();
        var otherAdmin = await AuthTestHelpers.CreateTenantAdminAsync(otherClient);
        AuthTestHelpers.AuthorizeAs(otherClient, otherAdmin.Tokens);
        var otherBranchResponse = await otherClient.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Other Tenant Branch" });
        var otherBranch = (await otherBranchResponse.Content.ReadFromJsonAsync<BranchResponse>())!;

        var response = await client.PostAsJsonAsync("/api/v1/warehouses", new CreateWarehouseRequest { BranchId = otherBranch.Id, Name = "Cross Tenant Warehouse" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateThenActivateBranch_RoundTrips()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var branchResponse = await client.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Kisumu" });
        var branch = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!;

        var deactivateResponse = await client.PostAsync($"/api/v1/branches/{branch.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/branches/{branch.Id}");
        var fetched = (await getResponse.Content.ReadFromJsonAsync<BranchResponse>())!;
        Assert.Equal("Inactive", fetched.Status);

        var activateResponse = await client.PostAsync($"/api/v1/branches/{branch.Id}/activate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);
    }
}
