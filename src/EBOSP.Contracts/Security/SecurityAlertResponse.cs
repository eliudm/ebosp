namespace EBOSP.Contracts.Security;

public sealed record SecurityAlertResponse(
    Guid Id,
    string Rule,
    string Severity,
    string Description,
    Guid? RelatedActorId,
    string? RelatedAggregateType,
    string? RelatedAggregateId,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? AcknowledgedByUserId,
    DateTimeOffset? AcknowledgedAt,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes);
