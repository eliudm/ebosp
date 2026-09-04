using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Sales quotations (dev guide §15) - accept/reject records the customer's own decision, so both need only "sales.create", the same as raising the quotation.</summary>
[Authorize]
public sealed class QuotationsController(IQuotationService quotationService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<QuotationResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await quotationService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await quotationService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<QuotationResponse>> Create(CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        var result = await quotationService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<QuotationResponse>> Accept(Guid id, CancellationToken cancellationToken) =>
        Ok(await quotationService.AcceptAsync(TenantId, UserId, id, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.SalesCreate)]
    public async Task<ActionResult<QuotationResponse>> Reject(Guid id, CancellationToken cancellationToken) =>
        Ok(await quotationService.RejectAsync(TenantId, UserId, id, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
