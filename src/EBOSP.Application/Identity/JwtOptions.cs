namespace EBOSP.Application.Identity;

/// <summary>
/// Bound from the "Jwt" configuration section. <see cref="SigningKey"/> is never committed to
/// appsettings - it comes from user-secrets locally and environment/Key Vault in deployed
/// environments (dev guide §14: "Do not place secrets in source control").
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    public int RefreshTokenLifetimeDays { get; init; } = 30;
}
