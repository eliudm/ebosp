using EBOSP.Application.Common;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests;

/// <summary>
/// Points at a real Postgres (dev guide §25.2: "Real test database"). Uses the same
/// ConnectionStrings__Default env var CI already provides for its Postgres service, falling back
/// to the local docker-compose default so `dotnet test` works unchanged on a dev machine.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=localhost;Port=5435;Database=ebosp;Username=ebosp;Password=ebosp_dev_only";

    // Arbitrary fixed key, same constant ApiTests' MigrationLock uses - both projects migrate the
    // same shared database from separate processes, so the lock only works if every caller across
    // every test project agrees on it.
    private const long MigrationAdvisoryLockKey = 483920;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext(new TestCurrentUserContext());
        // Every other test project (ApiTests/E2ETests) also migrates this same shared test
        // database from its own factory instances at roughly the same time. Against a fresh,
        // unmigrated database (what CI's Postgres service container is on every run) two racing
        // migrators can both start creating the same tables, so the loser fails with a Postgres
        // error even though the schema ends up correct either way - a session advisory lock
        // serializes the actual migration work instead of trying to catch every collision after
        // the fact.
        await context.Database.OpenConnectionAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({MigrationAdvisoryLockKey});");
            await context.Database.MigrateAsync();
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({MigrationAdvisoryLockKey});");
            await context.Database.CloseConnectionAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AppDbContext CreateContext(ICurrentUserContext currentUserContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options;
        return new AppDbContext(options, currentUserContext);
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
