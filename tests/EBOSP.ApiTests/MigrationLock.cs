using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.ApiTests;

/// <summary>
/// Every test class in this project (and EBOSP.E2ETests, which reuses this factory) gets its own
/// <see cref="CustomWebApplicationFactory"/> instance, each calling <c>MigrateAsync()</c> against
/// the same shared test database. Against a fresh, unmigrated database (exactly what CI's
/// Postgres service container is on every run - a local dev database that already has every
/// migration applied never exercises this race at all) two racing migrators can both observe "no
/// pending migrations applied yet" and both start creating the same tables, so the loser fails with
/// a Postgres error (duplicate __EFMigrationsHistory row, or "relation already exists", depending
/// on exactly where the race lands) even though the schema ends up correct either way. A Postgres
/// session advisory lock serializes the actual migration work across every concurrent caller
/// instead of trying to catch every possible collision error code after the fact.
/// </summary>
internal static class MigrationLock
{
    // Arbitrary fixed key - only needs to be the same constant across every caller sharing this database.
    private const long AdvisoryLockKey = 483920;

    public static async Task MigrateSerializedAsync(AppDbContext context)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({AdvisoryLockKey});");
            await context.Database.MigrateAsync();
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey});");
            await context.Database.CloseConnectionAsync();
        }
    }
}
