using EBOSP.Contracts.Common;

namespace EBOSP.Application.Audit;

public sealed record AuditEventRecord(
    Guid Id,
    string EventType,
    Guid? ActorId,
    string AggregateType,
    string AggregateId,
    DateTimeOffset OccurredAt,
    Guid? CorrelationId,
    string Payload);

public interface IAuditEventRepository
{
    Task<PagedResult<AuditEventRecord>> ListAsync(
        Guid tenantId,
        Guid? actorId,
        string? eventType,
        string? aggregateType,
        string? aggregateId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        PagedRequest request,
        CancellationToken cancellationToken);
}
