using System.Security.Claims;
using EBOSP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EBOSP.UnitTests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private const string Code = "user.manage";

    [Fact]
    public async Task HandleRequirementAsync_UserHasPermissionClaim_Succeeds()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext(new Claim(PermissionClaimTypes.Permission, Code), authenticated: true);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserMissingPermissionClaim_DoesNotSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext(new Claim(PermissionClaimTypes.Permission, "other.permission"), authenticated: true);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_Unauthenticated_DoesNotSucceed()
    {
        // Authorization test matrix: "Tampered client-side role - server ignores client claim
        // changes not backed by trusted identity" - an unauthenticated principal has no trusted claims.
        var handler = new PermissionAuthorizationHandler();
        var context = BuildContext(new Claim(PermissionClaimTypes.Permission, Code), authenticated: false);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext BuildContext(Claim claim, bool authenticated)
    {
        var identity = authenticated ? new ClaimsIdentity([claim], "TestAuth") : new ClaimsIdentity([claim]);
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(Code);

        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }
}
