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
/// the suite never depends on user-secrets.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                    ?? "Host=localhost;Port=5435;Database=ebosp;Username=ebosp;Password=ebosp_dev_only",
                ["Jwt:Issuer"] = "ebosp-tests",
                ["Jwt:Audience"] = "ebosp-tests",
                ["Jwt:SigningKey"] = "api-tests-signing-key-not-a-real-secret-do-not-reuse-1234567890",
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
