using EBOSP.Api.Authorization;
using EBOSP.Application.Billing;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Contracts.Billing;
using EBOSP.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Payment intent/confirmation (dev guide §16) - creating an intent needs only "payment.create"; confirming one above the configured threshold additionally needs "payment.create.large" (spec §15: "Finance role + transaction limit").</summary>
[Authorize]
public sealed class PaymentsController(
    IPaymentService paymentService,
    ICurrentUserContext currentUser,
    IAuthorizationService authorizationService,
    BillingOptions billingOptions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await paymentService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await paymentService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.PaymentCreate)]
    public async Task<ActionResult<PaymentResponse>> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.PaymentCreate)]
    public async Task<ActionResult<PaymentResponse>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var payment = await paymentService.GetByIdAsync(TenantId, id, cancellationToken);

        // Whether payment.create.large is additionally required depends on the persisted payment's
        // amount, so it can't be expressed as a static [Authorize(Policy=...)] attribute - checked
        // imperatively, same as InventoryController.Adjust and PurchaseRequestsController.Approve.
        if (payment.Amount > billingOptions.LargePaymentThreshold)
        {
            var largePaymentAuthorization = await authorizationService.AuthorizeAsync(
                User, PermissionPolicyProvider.PolicyPrefix + PermissionCodes.PaymentCreateLarge);
            if (!largePaymentAuthorization.Succeeded)
            {
                return Forbid();
            }
        }

        return Ok(await paymentService.ConfirmAsync(TenantId, UserId, id, cancellationToken));
    }

    [HttpPost("{id:guid}/fail")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.PaymentCreate)]
    public async Task<ActionResult<PaymentResponse>> Fail(Guid id, CancellationToken cancellationToken) =>
        Ok(await paymentService.FailAsync(TenantId, UserId, id, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
