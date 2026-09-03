using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Per-warehouse reorder threshold overrides - gated by master-data.manage, not an inventory
/// permission (dev guide §12 groups "reorder rules" under master data configuration, not §13's
/// operational concerns).
/// </summary>
[Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
[Route("api/v{version:apiVersion}/reorder-rules")]
public sealed class ReorderRulesController(IReorderRuleService reorderRuleService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReorderRuleResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await reorderRuleService.ListAsync(TenantId, request, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ReorderRuleResponse>> Create(CreateReorderRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await reorderRuleService.CreateAsync(TenantId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReorderRuleResponse>> Update(Guid id, UpdateReorderRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await reorderRuleService.UpdateAsync(TenantId, id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await reorderRuleService.DeleteAsync(TenantId, id, cancellationToken);
        return NoContent();
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");
}
