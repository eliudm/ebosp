using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>Product category management - reads need only authentication, writes require "master-data.manage" (dev guide §12).</summary>
[Authorize]
[Route("api/v{version:apiVersion}/product-categories")]
public sealed class ProductCategoriesController(IProductCategoryService categoryService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductCategoryResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await categoryService.ListAsync(TenantId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductCategoryResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await categoryService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<ProductCategoryResponse>> Create(CreateProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(TenantId, UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<ActionResult<ProductCategoryResponse>> Update(Guid id, UpdateProductCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await categoryService.UpdateAsync(TenantId, UserId, id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.ActivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionPolicyProvider.PolicyPrefix + PermissionCodes.MasterDataManage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeactivateAsync(TenantId, UserId, id, cancellationToken);
        return NoContent();
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
