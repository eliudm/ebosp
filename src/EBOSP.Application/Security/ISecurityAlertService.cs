using EBOSP.Contracts.Common;
using EBOSP.Contracts.Security;

namespace EBOSP.Application.Security;

/// <summary>No CreateAsync - alerts are exclusively raised by detection logic in other services, never by a client (dev guide §18: "do not expose audit mutation endpoints to ordinary users", extended here to alerts).</summary>
public interface ISecurityAlertService
{
    Task<SecurityAlertResponse> AcknowledgeAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<SecurityAlertResponse> InvestigateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken);

    Task<SecurityAlertResponse> ResolveAsync(Guid tenantId, Guid actingUserId, Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken);

    Task<SecurityAlertResponse> MarkFalsePositiveAsync(Guid tenantId, Guid actingUserId, Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken);

    Task<SecurityAlertResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SecurityAlertResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
