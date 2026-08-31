namespace EBOSP.Application.Identity;

public enum PasswordVerificationResult
{
    Failed,
    Success,
    SuccessRehashNeeded,
}

/// <summary>
/// Wraps the identity framework's approved password hashing implementation (spec §14) so
/// Application-layer code never depends on the concrete hashing library directly.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string hash, string password);
}
