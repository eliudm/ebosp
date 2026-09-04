using System.Net;
using EBOSP.ApiTests;

namespace EBOSP.E2ETests.Journeys;

/// <summary>
/// Dev guide §25.3 "Login -> dashboard". The frontend's DashboardPage is currently a placeholder
/// that renders "Signed in as {email}" purely from client-side state - it makes no API call of its
/// own (see frontend/src/app/DashboardPage.tsx) - so the backend-testable content of this journey
/// is that login actually produces a working session: a token pair usable for a real authenticated
/// request, not just a 200 from /auth/login. Also proves the other half of the journey - the same
/// request is rejected before login, matching the frontend's route-guard redirect to /login.
/// </summary>
public class LoginToDashboardTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_ThenAnyAuthenticatedRequest_Succeeds_AndFailsBeforeLogin()
    {
        var client = factory.CreateClient();

        var beforeLogin = await client.GetAsync("/api/v1/branches");
        Assert.Equal(HttpStatusCode.Unauthorized, beforeLogin.StatusCode);

        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        Assert.NotEmpty(admin.Tokens.AccessToken);
        Assert.NotEmpty(admin.Tokens.RefreshToken);
        Assert.True(admin.Tokens.AccessTokenExpiresAt > DateTimeOffset.UtcNow);

        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var afterLogin = await client.GetAsync("/api/v1/branches");

        Assert.Equal(HttpStatusCode.OK, afterLogin.StatusCode);
    }
}
