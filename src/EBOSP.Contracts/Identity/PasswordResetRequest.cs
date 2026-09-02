using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class PasswordResetRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
