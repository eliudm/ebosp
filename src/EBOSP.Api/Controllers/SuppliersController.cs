using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Vendor master - reads need only authentication, writes require "master-data.manage" (same grouping rationale as ReorderRulesController).</summary>
[Authorize]
public sealed class SuppliersController(ISupplierService supplierService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await supplierService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await supplierService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await supplierService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken) =>
        Ok(await supplierService.UpdateAsync(TenantId, UserId, id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await supplierService.ActivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await supplierService.DeactivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
