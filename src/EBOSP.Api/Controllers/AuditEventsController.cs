using EBOSP.Api.Authorization;
using EBOSP.Application.Audit;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Contracts.Audit;
using EBOSP.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Read-only audit search over the outbox (dev guide §18) - gated by "audit.read" on the read itself, not just writes: "do not expose audit mutation endpoints to ordinary users" extends to not exposing the log at all to them.</summary>
[Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.AuditRead)]
[Route("api/v{version:apiVersion}/audit-events")]
public sealed class AuditEventsController(IAuditEventQueryService auditEvents, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditEventResponse>>> List(
        [FromQuery] Guid? actorId,
        [FromQuery] string? eventType,
        [FromQuery] string? aggregateType,
        [FromQuery] string? aggregateId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken) =>
        Ok(await auditEvents.ListAsync(TenantId, actorId, eventType, aggregateType, aggregateId, from, to, request, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");
}
