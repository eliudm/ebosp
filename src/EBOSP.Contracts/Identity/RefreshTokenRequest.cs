using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
