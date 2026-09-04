using EBOSP.Application.Assistant;
using EBOSP.Application.Common;
using EBOSP.Contracts.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Read-only natural-language business queries (spec §26, dev guide §43) - gated by [Authorize]
/// only; each of the four underlying tools enforces its own actual authorization inside
/// AssistantService (three need nothing beyond authentication, one needs audit.read), since no
/// single permission code covers all four.
/// </summary>
[Authorize]
[Route("api/v{version:apiVersion}/assistant")]
public sealed class AssistantController(IAssistantService assistant, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<AskAssistantResponse>> Ask(AskAssistantRequest request, CancellationToken cancellationToken) =>
        Ok(await assistant.AskAsync(TenantId, UserId, request, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
