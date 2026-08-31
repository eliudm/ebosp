using System.Security.Cryptography;

namespace EBOSP.Application.Identity;

/// <summary>
/// Generates the opaque refresh token handed to the client and the hash stored server-side
/// (spec §11: revocable refresh/session handling) - only the hash is ever persisted.
/// </summary>
public static class RefreshTokenGenerator
{
    public static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
