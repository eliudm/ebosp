using EBOSP.Api.Authorization;
using EBOSP.Application.Billing;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Generates invoices from fulfilled sales orders (dev guide §15) - gated by "invoice.create", matching spec's Finance Officer ownership of invoices.</summary>
[Authorize]
public sealed class InvoicesController(IInvoiceService invoiceService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await invoiceService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await invoiceService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InvoiceCreate)]
    public async Task<ActionResult<InvoiceResponse>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await invoiceService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
