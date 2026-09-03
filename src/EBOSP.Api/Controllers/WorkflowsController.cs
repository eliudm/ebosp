using EBOSP.Application.Common;
using EBOSP.Application.Procurement;
using EBOSP.Contracts.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Generic approval workflow lookup (spec's literal GET /workflows/{id} endpoint) - read-only, tenant-scoped.</summary>
[Authorize]
public sealed class WorkflowsController(IWorkflowService workflowService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowInstanceResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await workflowService.GetByIdAsync(TenantId, id, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");
}
