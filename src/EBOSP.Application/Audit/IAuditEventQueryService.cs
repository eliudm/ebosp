using EBOSP.Contracts.Audit;
using EBOSP.Contracts.Common;

namespace EBOSP.Application.Audit;

public interface IAuditEventQueryService
{
    Task<PagedResult<AuditEventResponse>> ListAsync(
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
