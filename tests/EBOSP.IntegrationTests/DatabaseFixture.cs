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

    public async Task InitializeAsync()
    {
        await using var context = CreateContext(new TestCurrentUserContext());
        // Every other test project (ApiTests/E2ETests) also migrates this same shared test
        // database from its own factory instances at roughly the same time - two racing to apply
        // the same not-yet-applied migration both try to insert the same __EFMigrationsHistory
        // row, and the loser gets a 23505 duplicate-key error even though the schema ends up
        // exactly where it should be either way. Safe to swallow.
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505" && ex.ConstraintName == "PK___EFMigrationsHistory")
        {
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
