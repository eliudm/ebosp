namespace EBOSP.Application.Identity;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Issues the short-lived JWT access token carrying tenant and permission claims (spec §11).</summary>
public interface IJwtTokenService
{
    IssuedAccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlyCollection<string> permissionCodes);
}
