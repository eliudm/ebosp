using EBOSP.Application.Assistant;
using EBOSP.Application.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBOSP.ApiTests;

/// <summary>
/// Boots the real Api host against a real Postgres (same connection-string convention as CI's
/// Postgres service - dev guide §25.2), with test-only Jwt configuration supplied in-memory so
/// the suite never depends on user-secrets. Rate limiting is disabled by default (see
/// <see cref="DisableRateLimiting"/>) since TestServer gives every in-process request the same
/// synthetic client IP, which would make the per-IP limiter collide across unrelated tests;
/// <see cref="RateLimitEnforcingWebApplicationFactory"/> opts back in for the one test class that
/// actually verifies the limiter itself.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Replaces the real (log-only) password reset notifier so tests can retrieve the cleartext
    /// token that would otherwise only ever reach the user - there is no email channel to
    /// intercept yet (dev guide §17 is a later milestone).
    /// </summary>
    public CapturingPasswordResetNotifier PasswordResetNotifier { get; } = new();

    /// <summary>Swaps out the real/unavailable IAiCompletionClient so assistant tests never call the real Anthropic API - see <see cref="FakeAiCompletionClient"/>.</summary>
    public FakeAiCompletionClient AiCompletionClient { get; } = new();

    protected virtual bool DisableRateLimiting => true;

    /// <summary>
    /// True in every factory except <see cref="AiUnavailableWebApplicationFactory"/>, which needs
    /// Program.cs's own real (Ai:ApiKey-unset) IAiCompletionClient resolution left untouched to
    /// prove the 503 path actually works, not just that the fake client stands in for it.
    /// </summary>
    protected virtual bool RegisterFakeAiClient => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                    ?? "Host=localhost;Port=5435;Database=ebosp;Username=ebosp;Password=ebosp_dev_only",
                ["Jwt:Issuer"] = "ebosp-tests",
                ["Jwt:Audience"] = "ebosp-tests",
                ["Jwt:SigningKey"] = "api-tests-signing-key-not-a-real-secret-do-not-reuse-1234567890",
            };

            if (DisableRateLimiting)
            {
                // Program.cs's AddPolicy calls can't be re-registered under the same name (it
                // throws on a duplicate), so tests that aren't exercising the limiter itself raise
                // the configurable permit limits instead of trying to replace the policies.
                settings["RateLimiting:login:PermitLimit"] = "1000000";
                settings["RateLimiting:password-reset:PermitLimit"] = "1000000";
                settings["RateLimiting:tenant-creation:PermitLimit"] = "1000000";
            }

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IPasswordResetNotifier>(PasswordResetNotifier);
            if (RegisterFakeAiClient)
            {
                services.AddSingleton<IAiCompletionClient>(AiCompletionClient);
            }
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await MigrationLock.MigrateSerializedAsync(context);
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
