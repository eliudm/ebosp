using System.Security.Cryptography;

namespace EBOSP.Application.Identity;

/// <summary>
/// Generates an opaque, single-use token handed to the client and the hash stored server-side
/// (spec §11: revocable refresh/session handling; password reset) - only the hash is ever
/// persisted. Shared by refresh tokens and password reset tokens.
/// </summary>
public static class SecureTokenGenerator
{
    public static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
