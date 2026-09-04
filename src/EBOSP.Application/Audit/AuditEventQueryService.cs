using EBOSP.Contracts.Audit;
using EBOSP.Contracts.Common;

namespace EBOSP.Application.Audit;

/// <summary>Read-only query over the existing outbox (dev guide §18) - no new AuditEvent table, since OutboxMessage already carries everything spec's AuditEvent ERD row needs.</summary>
public sealed class AuditEventQueryService(IAuditEventRepository events) : IAuditEventQueryService
{
    public async Task<PagedResult<AuditEventResponse>> ListAsync(
        Guid tenantId,
        Guid? actorId,
        string? eventType,
        string? aggregateType,
        string? aggregateId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = await events.ListAsync(tenantId, actorId, eventType, aggregateType, aggregateId, from, to, request, cancellationToken);
        return new PagedResult<AuditEventResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private static AuditEventResponse ToResponse(AuditEventRecord record) => new(
        record.Id, record.EventType, record.ActorId, record.AggregateType, record.AggregateId, record.OccurredAt,
        AuditEventSeverity.Classify(record.EventType), record.CorrelationId, record.Payload);
}
