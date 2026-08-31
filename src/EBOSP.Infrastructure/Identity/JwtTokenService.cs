using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EBOSP.Application.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EBOSP.Infrastructure.Identity;

/// <summary>Issues the short-lived JWT access token (spec §10.4: "Bearer token").</summary>
public sealed class JwtTokenService(JwtOptions options, IClock clock) : IJwtTokenService
{
    public IssuedAccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlyCollection<string> permissionCodes)
    {
        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(permissionCodes.Select(code => new Claim(PermissionClaimTypes.Permission, code)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAccessToken(value, expiresAt);
    }
}
