namespace EBOSP.Contracts.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string? Body,
    string? RelatedAggregateType,
    string? RelatedAggregateId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
