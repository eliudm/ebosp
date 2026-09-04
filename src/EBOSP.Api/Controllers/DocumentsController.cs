using EBOSP.Api.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Documents;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Attached-file metadata and content (spec §18). Upload is gated by the parent entity's own write
/// permission, not a blanket "document.manage" permission - checked imperatively since which
/// permission applies depends on the request's EntityType, the same shape every other
/// content-dependent check in this app already uses (InventoryController.Adjust,
/// PurchaseRequestsController.Approve, PaymentsController.Confirm). Reads only need
/// authentication, mirroring the two supported parent entities' own current read gates.
/// </summary>
[Authorize]
public sealed class DocumentsController(
    IDocumentService documentService,
    ICurrentUserContext currentUser,
    IAuthorizationService authorizationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentResponse>>> List([FromQuery] string entityType, [FromQuery] string entityId, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await documentService.ListForEntityAsync(TenantId, entityType, entityId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await documentService.GetByIdAsync(TenantId, id, cancellationToken));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        var (metadata, content) = await documentService.GetContentAsync(TenantId, id, cancellationToken);
        return File(content, metadata.ContentType, metadata.FileName);
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<DocumentResponse>> Upload([FromForm] string entityType, [FromForm] string entityId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!DocumentEntityTypes.WritePermissionByEntityType.TryGetValue(entityType, out var requiredPermission))
        {
            return BadRequest($"Unsupported entity type '{entityType}'.");
        }

        var authorization = await authorizationService.AuthorizeAsync(User, PermissionPolicyProvider.PolicyPrefix + requiredPermission);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await using var stream = file.OpenReadStream();
        var result = await documentService.UploadAsync(TenantId, UserId, entityType, entityId, file.FileName, file.ContentType, stream, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
