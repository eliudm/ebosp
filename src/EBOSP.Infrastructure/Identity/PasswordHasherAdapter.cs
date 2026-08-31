using EBOSP.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using AppPasswordVerificationResult = EBOSP.Application.Identity.PasswordVerificationResult;
using IAppPasswordHasher = EBOSP.Application.Identity.IPasswordHasher;

namespace EBOSP.Infrastructure.Identity;

/// <summary>Wraps the identity framework's approved password hashing implementation (spec §14).</summary>
public sealed class PasswordHasherAdapter : IAppPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(user: null!, password);

    public AppPasswordVerificationResult Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(user: null!, hash, password) switch
        {
            PasswordVerificationResult.Success => AppPasswordVerificationResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => AppPasswordVerificationResult.SuccessRehashNeeded,
            _ => AppPasswordVerificationResult.Failed,
        };
}
