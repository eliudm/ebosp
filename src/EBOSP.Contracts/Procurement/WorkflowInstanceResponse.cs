namespace EBOSP.Contracts.Procurement;

public sealed record WorkflowInstanceResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? DecidedByUserId,
    DateTimeOffset? DecidedAt,
    string? DecisionNotes);
