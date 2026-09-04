using System.Text.Json;
using EBOSP.Application.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Outbox;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EBOSP.Worker;

/// <summary>
/// Implements dev guide §19.1's long-deferred outbox background publisher: reads unprocessed
/// rows, "publishes" them (here: creates an in-app Notification when the payload carries
/// NotifyUserId/NotificationTitle - dev guide §20, "produced from domain events rather than
/// embedded throughout business services"), marks PublishedAt, retries transient failures with a
/// bounded attempt count (a lightweight dead-letter treatment - §19.1 step 9), and is an
/// idempotent consumer (§19.2: "assume at-least-once delivery") via the unique
/// Notification(TenantId, SourceOutboxMessageId) index.
///
/// Runs in EBOSP.Worker, a plain Generic Host with no HttpContext, so ICurrentUserContext is
/// always empty here - every tenant-scoped query below uses IgnoreQueryFilters() plus an explicit
/// tenant Where, the same idiom UserRepository's ...IgnoringTenantAsync methods already use
/// before a tenant is known. Each message gets its own DbContext scope, so a failure on one never
/// leaves tracked-but-uncommitted state that could contaminate the next (the exact class of bug
/// M6/M8 each found and fixed elsewhere).
/// </summary>
public sealed class NotificationOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<NotificationOutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification outbox processing pass failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Exposed publicly so integration tests can drive one processing pass directly, without hosting the BackgroundService's infinite poll loop.</summary>
    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        List<Guid> messageIds;
        using (var listScope = scopeFactory.CreateScope())
        {
            var listContext = listScope.ServiceProvider.GetRequiredService<AppDbContext>();
            messageIds = await listContext.OutboxMessages
                .Where(m => m.PublishedAt == null && m.AttemptCount < MaxAttempts)
                .OrderBy(m => m.OccurredAt)
                .Take(BatchSize)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);
        }

        foreach (var messageId in messageIds)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await ProcessOneMessageAsync(context, messageId, cancellationToken);
        }
    }

    private async Task ProcessOneMessageAsync(AppDbContext context, Guid messageId, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages.SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        try
        {
            await TryCreateNotificationAsync(context, message, cancellationToken);
            message.PublishedAt = clock.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to process outbox message {MessageId}.", messageId);
            var errorText = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await context.OutboxMessages
                .Where(m => m.Id == messageId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.AttemptCount, m => m.AttemptCount + 1).SetProperty(m => m.LastError, errorText),
                    cancellationToken);
        }
    }

    private static async Task TryCreateNotificationAsync(AppDbContext context, OutboxMessage message, CancellationToken cancellationToken)
    {
        using var payload = JsonDocument.Parse(message.Payload);
        if (!payload.RootElement.TryGetProperty("NotifyUserId", out var notifyUserIdElement) ||
            notifyUserIdElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(notifyUserIdElement.GetString(), out var recipientUserId))
        {
            return;
        }

        if (!payload.RootElement.TryGetProperty("NotificationTitle", out var titleElement) ||
            titleElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(titleElement.GetString()))
        {
            return;
        }

        var alreadyExists = await context.Notifications
            .IgnoreQueryFilters()
            .AnyAsync(n => n.TenantId == message.TenantId && n.SourceOutboxMessageId == message.Id, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        var notification = Notification.Create(
            message.TenantId, recipientUserId, titleElement.GetString()!, body: null,
            message.AggregateType, message.AggregateId, message.Id, DateTimeOffset.UtcNow);
        await context.Notifications.AddAsync(notification, cancellationToken);
    }
}
