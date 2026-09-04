using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// In-app notification for one recipient (dev guide §20: "produced from domain events rather than
/// embedded throughout business services"). Created by the outbox background processor (M9,
/// EBOSP.Worker), never directly by a controller. SourceOutboxMessageId backs the idempotent-
/// consumer guarantee dev guide §19.2 requires (at-least-once delivery assumed).
/// </summary>
public sealed class Notification : Entity, ITenantOwned
{
    private Notification()
    {
    }

    public static Notification Create(
        Guid tenantId,
        Guid recipientUserId,
        string title,
        string? body,
        string? relatedAggregateType,
        string? relatedAggregateId,
        Guid sourceOutboxMessageId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body,
            RelatedAggregateType = relatedAggregateType,
            RelatedAggregateId = relatedAggregateId,
            SourceOutboxMessageId = sourceOutboxMessageId,
            CreatedAt = createdAt,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid RecipientUserId { get; private init; }

    public string Title { get; private init; } = null!;

    public string? Body { get; private init; }

    public string? RelatedAggregateType { get; private init; }

    public string? RelatedAggregateId { get; private init; }

    public Guid SourceOutboxMessageId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? ReadAt { get; private set; }

    public void MarkRead(DateTimeOffset now)
    {
        if (ReadAt is not null)
        {
            throw new InvalidOperationException("This notification is already marked read.");
        }

        ReadAt = now;
    }
}
