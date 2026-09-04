using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Converts an accepted quotation into a sales order, reserving stock (dev guide §15) - gated by "sales.create", same as raising the originating quotation.</summary>
[Authorize]
[Route("api/v{version:apiVersion}/sales-orders")]
public sealed class SalesOrdersController(ISalesOrderService salesOrderService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesOrderResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await salesOrderService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await salesOrderService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<SalesOrderResponse>> Create(CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await salesOrderService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
