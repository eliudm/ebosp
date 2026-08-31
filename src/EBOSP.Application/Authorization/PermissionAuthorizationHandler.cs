using Microsoft.AspNetCore.Authorization;

namespace EBOSP.Application.Authorization;

/// <summary>
/// Checks a <see cref="PermissionRequirement"/> against permission claims already baked into the
/// caller's access token at login - a tampered client-side role/claim never reaches here, because
/// the token is server-signed (authorization test matrix: "Server ignores client claim changes
/// not backed by trusted identity").
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.Claims.Any(c => c.Type == PermissionClaimTypes.Permission && c.Value == requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
