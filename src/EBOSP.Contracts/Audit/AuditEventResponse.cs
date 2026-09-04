namespace EBOSP.Contracts.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    string EventType,
    Guid? ActorId,
    string AggregateType,
    string AggregateId,
    DateTimeOffset OccurredAt,
    string Severity,
    Guid? CorrelationId,
    string Payload);
