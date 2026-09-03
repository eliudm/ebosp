using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Converts an approved purchase request into a purchase order (dev guide §14) - gated by "procurement.create", same as raising the originating request.</summary>
[Authorize]
[Route("api/v{version:apiVersion}/purchase-orders")]
public sealed class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await purchaseOrderService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await purchaseOrderService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.ProcurementCreate)]
    public async Task<ActionResult<PurchaseOrderResponse>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
