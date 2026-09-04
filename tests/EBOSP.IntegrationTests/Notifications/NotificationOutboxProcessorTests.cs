using EBOSP.Application.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Common;
using EBOSP.Infrastructure.Outbox;
using EBOSP.Infrastructure.Persistence;
using EBOSP.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EBOSP.IntegrationTests.Notifications;

/// <summary>
/// Drives the real outbox background processor (EBOSP.Worker) against real Postgres, no HTTP
/// involved - proves the idempotent-consumer guarantee (dev guide §19.2) directly: running the
/// same batch twice must never produce a second Notification for the same source message.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class NotificationOutboxProcessorTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=localhost;Port=5435;Database=ebosp;Username=ebosp;Password=ebosp_dev_only";

    [Fact]
    public async Task ProcessBatchAsync_MessageWithNotifyFields_CreatesExactlyOneNotification()
    {
        var tenant = Tenant.Create("Worker Tenant " + Guid.NewGuid());
        var recipientUserId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "TestNotifiableEvent",
            TenantId = tenant.Id,
            CorrelationId = null,
            CausationId = null,
            ActorId = Guid.NewGuid(),
            AggregateType = "Test",
            AggregateId = Guid.NewGuid().ToString(),
            Version = 1,
            Payload = $"{{\"NotifyUserId\":\"{recipientUserId}\",\"NotificationTitle\":\"Hello\"}}",
            OccurredAt = DateTimeOffset.UtcNow,
        };

        await using (var seedContext = CreateContext())
        {
            seedContext.Tenants.Add(tenant);
            seedContext.OutboxMessages.Add(message);
            await seedContext.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        // The full test suite runs concurrently against this same shared Postgres and constantly
        // inserts its own outbox rows, so this message isn't guaranteed to land in the very first
        // 50-row batch (oldest-first) - drain repeatedly until the worker actually reaches it,
        // exactly how a real background poller is expected to behave, rather than asserting after
        // exactly one pass.
        await DrainUntilPublishedAsync(processor, message.Id);
        // Running the same drain again must not create a duplicate - the message is already
        // PublishedAt, but exercising this twice is exactly what dev guide §19.2's "assume
        // at-least-once delivery" guards against if a message were ever reprocessed.
        await processor.ProcessBatchAsync(CancellationToken.None);

        await using var verifyContext = CreateContext();
        var notifications = await verifyContext.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.SourceOutboxMessageId == message.Id)
            .ToListAsync();
        var notification = Assert.Single(notifications);
        Assert.Equal(recipientUserId, notification.RecipientUserId);
        Assert.Equal("Hello", notification.Title);
    }

    [Fact]
    public async Task ProcessBatchAsync_MessageWithoutNotifyFields_MarksPublishedWithoutNotification()
    {
        var tenant = Tenant.Create("Worker Tenant " + Guid.NewGuid());
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "SomeOtherEvent",
            TenantId = tenant.Id,
            CorrelationId = null,
            CausationId = null,
            ActorId = null,
            AggregateType = "Test",
            AggregateId = Guid.NewGuid().ToString(),
            Version = 1,
            Payload = "{\"SomeField\":\"value\"}",
            OccurredAt = DateTimeOffset.UtcNow,
        };

        await using (var seedContext = CreateContext())
        {
            seedContext.Tenants.Add(tenant);
            seedContext.OutboxMessages.Add(message);
            await seedContext.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        await DrainUntilPublishedAsync(processor, message.Id);

        await using var verifyContext = CreateContext();
        var notifications = await verifyContext.Notifications.IgnoreQueryFilters().Where(n => n.SourceOutboxMessageId == message.Id).ToListAsync();
        Assert.Empty(notifications);
    }

    /// <summary>Repeatedly runs a processing pass until the target message is marked published, bounded so a genuine bug still fails the test instead of hanging.</summary>
    private async Task DrainUntilPublishedAsync(NotificationOutboxProcessor processor, Guid messageId)
    {
        const int maxIterations = 100;
        for (var i = 0; i < maxIterations; i++)
        {
            await processor.ProcessBatchAsync(CancellationToken.None);

            await using var context = CreateContext();
            var published = await context.OutboxMessages.Where(m => m.Id == messageId).Select(m => m.PublishedAt).SingleAsync();
            if (published is not null)
            {
                return;
            }
        }

        Assert.Fail($"Message {messageId} was not published after {maxIterations} processing passes.");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options;
        return new AppDbContext(options, new TestCurrentUserContext());
    }

    private NotificationOutboxProcessor CreateProcessor()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_connectionString));
        services.AddScoped<ICurrentUserContext, TestCurrentUserContext>();
        var provider = services.BuildServiceProvider();

        return new NotificationOutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new SystemClock(),
            NullLogger<NotificationOutboxProcessor>.Instance);
    }
}
