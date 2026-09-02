using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class PasswordResetConfirmRequest
{
    [Required]
    public required string Token { get; init; }

    [Required]
    [MinLength(10)]
    public required string NewPassword { get; init; }
}
