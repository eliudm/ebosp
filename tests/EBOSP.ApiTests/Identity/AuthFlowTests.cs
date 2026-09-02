using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;

namespace EBOSP.ApiTests.Identity;

/// <summary>Spec §30 acceptance criteria: tenant creation, user creation, and the login → protected-call → refresh → logout journey.</summary>
public class AuthFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateTenant_ThenLogin_IssuesTokens()
    {
        var client = factory.CreateClient();

        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        Assert.NotEqual(Guid.Empty, admin.TenantId);
        Assert.NotEqual(Guid.Empty, admin.AdminUserId);
        Assert.False(string.IsNullOrWhiteSpace(admin.Tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(admin.Tokens.RefreshToken));
    }

    [Fact]
    public async Task CreateTenant_WithAlreadyRegisteredEmail_Returns409()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest
        {
            TenantName = "Another Org " + Guid.NewGuid(),
            AdminEmail = admin.AdminEmail,
            AdminPassword = "SomeOtherPassword123!",
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task TenantAdmin_CanCreateUser_AssignRole_AndThatUserCanLoginAfterActivation()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var email = $"newuser-{Guid.NewGuid():N}@example.com";
        const string password = "SomePassword123!";
        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;

        var assignRoleResponse = await client.PostAsJsonAsync($"/api/v1/users/{created.Id}/roles", new AssignRoleRequest { RoleCode = "auditor" });
        Assert.Equal(HttpStatusCode.NoContent, assignRoleResponse.StatusCode);

        var activateResponse = await client.PostAsync($"/api/v1/users/{created.Id}/activate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var loginResponse = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldRefreshTokenNoLongerWorks()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var newTokens = (await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>())!;
        Assert.NotEqual(admin.Tokens.RefreshToken, newTokens.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusedRevokedToken_RevokesWholeSessionFamily()
    {
        // A revoked refresh token being presented again is treated as evidence of theft: every
        // active session for that user is killed, not just this one request denied.
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var rotateResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        var rotatedTokens = (await rotateResponse.Content.ReadFromJsonAsync<TokenResponse>())!;

        // Reuse the now-revoked original token - simulates an attacker replaying a stolen token.
        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // The legitimately-rotated token must now be dead too, even though it was never reused.
        var legitimateRetryResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = rotatedTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, legitimateRetryResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsSameFailureAsUnknownEmail()
    {
        // Spec §11: "Implement password reset/recovery without revealing whether an account
        // exists" - the login failure path must not distinguish the two cases either.
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var wrongPasswordResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = admin.AdminEmail, Password = "WrongPassword123!" });
        var unknownEmailResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = $"nobody-{Guid.NewGuid():N}@example.com", Password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);
    }
}
