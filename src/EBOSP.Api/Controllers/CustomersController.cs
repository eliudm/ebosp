using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Sales counterparty master - reads need only authentication, writes require "sales.create" (spec §4: Customer is the Sales Officer's stated scope, unlike Supplier which has no named owner).</summary>
[Authorize]
public sealed class CustomersController(ICustomerService customerService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await customerService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await customerService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await customerService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken) =>
        Ok(await customerService.UpdateAsync(TenantId, UserId, id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await customerService.ActivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await customerService.DeactivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
