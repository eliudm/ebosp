using EBOSP.Application.Identity;
using EBOSP.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EBOSP.Api.Controllers;

/// <summary>Self-service tenant onboarding (spec §30: "a new tenant can be created and isolated from other tenants").</summary>
public sealed class TenantsController(ITenantService tenantService) : ApiControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.TenantCreation)]
    public async Task<ActionResult<TenantResponse>> Create(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantService.CreateTenantAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { result.TenantId }, result);
    }
}
