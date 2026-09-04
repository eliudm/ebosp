using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Ships a sales order (dev guide §15: "Delivery is a separate auditable state transition") - gated by "sales.fulfill", separate from "sales.create" since this is the action that actually moves physical stock.</summary>
[Authorize]
public sealed class DeliveriesController(IDeliveryService deliveryService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<DeliveryResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await deliveryService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await deliveryService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesFulfill)]
    public async Task<ActionResult<DeliveryResponse>> Create(CreateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
