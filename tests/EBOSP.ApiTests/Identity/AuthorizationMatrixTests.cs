using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EBOSP.ApiTests.Identity;

/// <summary>
/// Dev guide §11.2 authorization test matrix. "User accesses unauthorized branch" is not covered
/// here - no branch-scoped resource exists yet in this pass (that lands with the modules that
/// actually have branch-scoped data, from Phase 3 onward); every other row has a concrete
/// endpoint in this module to exercise it against.
/// </summary>
public class AuthorizationMatrixTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = "x@example.com", Password = "SomePassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutPermission_Returns403()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        // A freshly created user has no roles/permissions assigned.
        var noRoleEmail = $"norole-{Guid.NewGuid():N}@example.com";
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = noRoleEmail, Password = "SomePassword123!" });
        createResponse.EnsureSuccessStatusCode();

        var noRoleClient = factory.CreateClient();
        var noRoleTokens = await AuthTestHelpers.LoginAsync(noRoleClient, noRoleEmail, "SomePassword123!");
        AuthTestHelpers.AuthorizeAs(noRoleClient, noRoleTokens);

        var response = await noRoleClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = "another@example.com", Password = "SomePassword123!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserAccessesAnotherTenantsResource_Denied()
    {
        var client = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(client);
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(factory.CreateClient());

        AuthTestHelpers.AuthorizeAs(client, tenantA.Tokens);
        var response = await client.PostAsync($"/api/v1/users/{tenantB.AdminUserId}/suspend", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserAttemptsSelfSuspend_Denied()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var response = await client.PostAsync($"/api/v1/users/{admin.AdminUserId}/suspend", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminPerformsHighRiskAction_AllowedAndAudited()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var targetEmail = $"target-{Guid.NewGuid():N}@example.com";
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = targetEmail, Password = "SomePassword123!" });
        var target = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;

        var suspendResponse = await client.PostAsync($"/api/v1/users/{target.Id}/suspend", content: null);

        Assert.Equal(HttpStatusCode.NoContent, suspendResponse.StatusCode);
        Assert.True(await HasOutboxEventAsync(admin.TenantId, "UserSuspended", target.Id));
    }

    [Fact]
    public async Task SuspendedUser_AttemptsLogin_DeniedAndAudited()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var targetEmail = $"suspended-{Guid.NewGuid():N}@example.com";
        const string targetPassword = "SomePassword123!";
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = targetEmail, Password = targetPassword });
        var target = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;
        await client.PostAsync($"/api/v1/users/{target.Id}/activate", content: null);
        await client.PostAsync($"/api/v1/users/{target.Id}/suspend", content: null);

        var anonymousClient = factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = targetEmail, Password = targetPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.True(await HasOutboxEventAsync(admin.TenantId, "LoginFailed", target.Id));
    }

    [Fact]
    public async Task ExpiredOrRevokedRefreshToken_Denied()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        logoutResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task TamperedAccessToken_Rejected()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        // Flip the last character of the signature segment - server-side signature validation
        // must reject it before any claim on it is trusted (authorization test matrix: "Server
        // ignores client claim changes not backed by trusted identity").
        var parts = admin.Tokens.AccessToken.Split('.');
        var tamperedSignature = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tamperedToken);
        var response = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = "wontexist@example.com", Password = "SomePassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<bool> HasOutboxEventAsync(Guid tenantId, string eventType, Guid aggregateId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.OutboxMessages.AnyAsync(m =>
            m.TenantId == tenantId && m.EventType == eventType && m.AggregateId == aggregateId.ToString());
    }
}
