namespace EBOSP.Infrastructure.Outbox;

/// <summary>
/// Persistence record for the transactional outbox (dev guide §19). Written in the same
/// database transaction as the business state change it describes; published asynchronously
/// by a background worker and never mutated by ordinary business code once written.
/// </summary>
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string EventType { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid? CorrelationId { get; init; }
    public required Guid? CausationId { get; init; }
    public required Guid? ActorId { get; init; }
    public required string AggregateType { get; init; }
    public required string AggregateId { get; init; }
    public required int Version { get; init; }
    public required string Payload { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
