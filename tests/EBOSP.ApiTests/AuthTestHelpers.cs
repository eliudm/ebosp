using System.Net.Http.Headers;
using System.Net.Http.Json;
using EBOSP.Contracts.Identity;

namespace EBOSP.ApiTests;

internal sealed record TenantAdminContext(Guid TenantId, Guid AdminUserId, string AdminEmail, string AdminPassword, TokenResponse Tokens);

internal static class AuthTestHelpers
{
    public const string AdminPassword = "SuperSecret123!";

    public static async Task<TenantAdminContext> CreateTenantAdminAsync(HttpClient client)
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";

        var createTenantResponse = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest
        {
            TenantName = "Test Tenant " + Guid.NewGuid(),
            AdminEmail = email,
            AdminPassword = AdminPassword,
        });
        createTenantResponse.EnsureSuccessStatusCode();
        var tenant = (await createTenantResponse.Content.ReadFromJsonAsync<TenantResponse>())!;

        var tokens = await LoginAsync(client, email, AdminPassword);

        return new TenantAdminContext(tenant.TenantId, tenant.AdminUserId, email, AdminPassword, tokens);
    }

    public static async Task<TokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    public static void AuthorizeAs(HttpClient client, TokenResponse tokens) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
}
