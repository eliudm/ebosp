using EBOSP.Application.Audit;
using EBOSP.Contracts.Common;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Audit;

public sealed class AuditEventRepository(AppDbContext context) : IAuditEventRepository
{
    public async Task<PagedResult<AuditEventRecord>> ListAsync(
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
        var query = context.OutboxMessages.Where(m => m.TenantId == tenantId);

        if (actorId is { } a)
        {
            query = query.Where(m => m.ActorId == a);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(m => m.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(aggregateType))
        {
            query = query.Where(m => m.AggregateType == aggregateType);
        }

        if (!string.IsNullOrWhiteSpace(aggregateId))
        {
            query = query.Where(m => m.AggregateId == aggregateId);
        }

        if (from is { } f)
        {
            query = query.Where(m => m.OccurredAt >= f);
        }

        if (to is { } t)
        {
            query = query.Where(m => m.OccurredAt <= t);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(m => m.OccurredAt) : query.OrderBy(m => m.OccurredAt);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new AuditEventRecord(m.Id, m.EventType, m.ActorId, m.AggregateType, m.AggregateId, m.OccurredAt, m.CorrelationId, m.Payload))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEventRecord> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
