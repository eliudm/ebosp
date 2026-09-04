using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Security;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Security alert investigation screen (dev guide §18.1) - system-raised only, no public create.
/// Unlike every other module's resources, reads themselves require "security.alert.manage", not
/// just the lifecycle actions - alert data is specifically about suspected-malicious activity.
/// </summary>
[Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SecurityAlertManage)]
[Route("api/v{version:apiVersion}/security/alerts")]
public sealed class SecurityAlertsController(ISecurityAlertService securityAlertService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SecurityAlertResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SecurityAlertResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<ActionResult<SecurityAlertResponse>> Acknowledge(Guid id, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.AcknowledgeAsync(TenantId, UserId, id, cancellationToken));

    [HttpPost("{id:guid}/investigate")]
    public async Task<ActionResult<SecurityAlertResponse>> Investigate(Guid id, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.InvestigateAsync(TenantId, UserId, id, cancellationToken));

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<SecurityAlertResponse>> Resolve(Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.ResolveAsync(TenantId, UserId, id, request, cancellationToken));

    [HttpPost("{id:guid}/false-positive")]
    public async Task<ActionResult<SecurityAlertResponse>> FalsePositive(Guid id, ResolveSecurityAlertRequest request, CancellationToken cancellationToken) =>
        Ok(await securityAlertService.MarkFalsePositiveAsync(TenantId, UserId, id, request, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
