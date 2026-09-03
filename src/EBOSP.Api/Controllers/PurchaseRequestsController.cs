using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Purchase requests and their approve/reject workflow (dev guide §14) - submit needs "procurement.create", approve/reject need "procurement.approve" (+ imperative "procurement.approve.large" above the configured threshold).</summary>
[Authorize]
[Route("api/v{version:apiVersion}/purchase-requests")]
public sealed class PurchaseRequestsController(
    IPurchaseRequestService purchaseRequestService,
    ICurrentUserContext currentUser,
    IAuthorizationService authorizationService,
    ProcurementOptions procurementOptions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseRequestResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await purchaseRequestService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await purchaseRequestService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.ProcurementCreate)]
    public async Task<ActionResult<PurchaseRequestResponse>> Create(CreatePurchaseRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await purchaseRequestService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.ProcurementApprove)]
    public async Task<ActionResult<PurchaseRequestResponse>> Approve(Guid id, ApprovePurchaseRequestRequest request, CancellationToken cancellationToken)
    {
        var purchaseRequest = await purchaseRequestService.GetByIdAsync(TenantId, id, cancellationToken);

        // Whether procurement.approve.large is additionally required depends on the persisted
        // request's estimated value, so it can't be expressed as a static [Authorize(Policy=...)]
        // attribute - checked imperatively, same as InventoryController.Adjust's threshold check.
        if (purchaseRequest.EstimatedValue > procurementOptions.HighValueThreshold)
        {
            var largeApprovalAuthorization = await authorizationService.AuthorizeAsync(
                User, PermissionPolicyProvider.PolicyPrefix + PermissionCodes.ProcurementApproveLarge);
            if (!largeApprovalAuthorization.Succeeded)
            {
                return Forbid();
            }
        }

        return Ok(await purchaseRequestService.ApproveAsync(TenantId, UserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.ProcurementApprove)]
    public async Task<ActionResult<PurchaseRequestResponse>> Reject(Guid id, RejectPurchaseRequestRequest request, CancellationToken cancellationToken) =>
        Ok(await purchaseRequestService.RejectAsync(TenantId, UserId, id, request, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
