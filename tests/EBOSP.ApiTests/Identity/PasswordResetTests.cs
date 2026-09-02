using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;

namespace EBOSP.ApiTests.Identity;

/// <summary>Spec §11: "Implement password reset/recovery without revealing whether an account exists."</summary>
public class PasswordResetTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task RequestReset_ForExistingAndUnknownEmail_ReturnsSameStatus()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var existingResponse = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new PasswordResetRequest { Email = admin.AdminEmail });
        var unknownResponse = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new PasswordResetRequest { Email = $"nobody-{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.Accepted, existingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unknownResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithValidToken_ChangesPasswordAndRevokesExistingSessions()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        var requestResponse = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new PasswordResetRequest { Email = admin.AdminEmail });
        requestResponse.EnsureSuccessStatusCode();
        var token = factory.PasswordResetNotifier.GetTokenFor(admin.AdminEmail);
        Assert.NotNull(token);

        const string newPassword = "BrandNewPassword123!";
        var confirmResponse = await client.PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new PasswordResetConfirmRequest { Token = token!, NewPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);

        // The refresh token issued at tenant creation must no longer work after a password reset.
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = admin.Tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        var oldPasswordLogin = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = admin.AdminEmail, Password = admin.AdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = admin.AdminEmail, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_TokenReuse_SecondAttemptDenied()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);

        await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new PasswordResetRequest { Email = admin.AdminEmail });
        var token = factory.PasswordResetNotifier.GetTokenFor(admin.AdminEmail)!;

        var firstConfirm = await client.PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new PasswordResetConfirmRequest { Token = token, NewPassword = "FirstNewPassword123!" });
        Assert.Equal(HttpStatusCode.NoContent, firstConfirm.StatusCode);

        var secondConfirm = await client.PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new PasswordResetConfirmRequest { Token = token, NewPassword = "SecondNewPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, secondConfirm.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithBogusToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new PasswordResetConfirmRequest { Token = "not-a-real-token", NewPassword = "SomePassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
