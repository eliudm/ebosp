using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Branch management - reads need only authentication, writes require "master-data.manage" (dev guide §12).</summary>
[Authorize]
public sealed class BranchesController(IBranchService branchService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BranchResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await branchService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BranchResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await branchService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<BranchResponse>> Create(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var result = await branchService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<BranchResponse>> Update(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken) =>
        Ok(await branchService.UpdateAsync(TenantId, UserId, id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await branchService.ActivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await branchService.DeactivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
