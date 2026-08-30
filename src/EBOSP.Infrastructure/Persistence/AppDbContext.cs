using EBOSP.Application.Common;
using EBOSP.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for the modular monolith (ADR-0001). Module-specific entity sets
/// are added here as each module is implemented, starting with Identity in Phase 2. Also serves
/// as the <see cref="IUnitOfWork"/> implementation - SaveChangesAsync already gives the
/// single-transaction-per-request semantics the abstraction needs (dev guide §9).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AggregateType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AggregateId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.TenantId);
        });
    }
}
