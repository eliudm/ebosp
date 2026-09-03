using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Stock movements and balances (dev guide §13) - reads need "inventory.read", writes need "inventory.adjust".</summary>
[Authorize]
[Route("api/v{version:apiVersion}/inventory")]
public sealed class InventoryController(
    IInventoryService inventoryService,
    ICurrentUserContext currentUser,
    IAuthorizationService authorizationService,
    InventoryOptions inventoryOptions) : ApiControllerBase
{
    [HttpGet("balances")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryRead)]
    public async Task<ActionResult<PagedResult<StockBalanceResponse>>> ListBalances(
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? productId, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await inventoryService.ListBalancesAsync(TenantId, warehouseId, productId, request, cancellationToken));

    [HttpGet("ledger")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryRead)]
    public async Task<ActionResult<PagedResult<StockLedgerEntryResponse>>> ListLedger(
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? productId, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await inventoryService.ListLedgerAsync(TenantId, warehouseId, productId, request, cancellationToken));

    [HttpPost("receipts")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<ActionResult<GoodsReceiptResponse>> Receive(ReceiveStockRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await inventoryService.ReceiveAsync(TenantId, UserId, request, cancellationToken));

    [HttpPost("issues")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<ActionResult<StockLedgerEntryResponse>> Issue(IssueStockRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await inventoryService.IssueAsync(TenantId, UserId, request, cancellationToken));

    [HttpPost("transfers")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<ActionResult<StockTransferResponse>> Transfer(TransferStockRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await inventoryService.TransferAsync(TenantId, UserId, request, cancellationToken));

    [HttpPost("reservations")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<ActionResult<StockReservationResponse>> Reserve(ReserveStockRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await inventoryService.ReserveAsync(TenantId, UserId, request, cancellationToken));

    [HttpPost("reservations/{id:guid}/release")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> ReleaseReservation(Guid id, CancellationToken cancellationToken)
    {
        await inventoryService.ReleaseReservationAsync(TenantId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjust)]
    public async Task<ActionResult<StockAdjustmentResponse>> Adjust(AdjustStockRequest request, CancellationToken cancellationToken)
    {
        // Whether inventory.adjust.large is additionally required depends on the request body, so
        // it can't be expressed as a static [Authorize(Policy=...)] attribute - checked imperatively.
        if (Math.Abs(request.QuantityDelta) > inventoryOptions.LargeAdjustmentThreshold)
        {
            var largeAdjustmentAuthorization = await authorizationService.AuthorizeAsync(
                User, PermissionPolicyProvider.PolicyPrefix + PermissionCodes.InventoryAdjustLarge);
            if (!largeAdjustmentAuthorization.Succeeded)
            {
                return Forbid();
            }
        }

        return StatusCode(StatusCodes.Status201Created, await inventoryService.AdjustAsync(TenantId, UserId, request, cancellationToken));
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
