using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;

namespace EBOSP.ApiTests.MasterData;

/// <summary>
/// Proves [Authorize] and the master-data.manage policy are applied consistently across all four
/// master data controllers, not just Branches (BranchesAndWarehousesTests only exercised that
/// one directly) - same behavior inferred from identical code isn't the same as proven.
/// </summary>
public class AuthorizationCoverageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    public static readonly TheoryData<string> ListEndpoints = new()
    {
        "/api/v1/branches",
        "/api/v1/warehouses",
        "/api/v1/product-categories",
        "/api/v1/products",
    };

    [Theory]
    [MemberData(nameof(ListEndpoints))]
    public async Task UnauthenticatedRequest_ToAnyResource_Returns401(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ListEndpoints))]
    public async Task WriteWithoutPermission_ToAnyResource_Returns403(string path)
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

        var response = await noRoleClient.PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
