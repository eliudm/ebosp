namespace EBOSP.Contracts.Identity;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
