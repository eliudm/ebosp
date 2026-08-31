using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(10)]
    public required string Password { get; init; }

    public Guid? PrimaryBranchId { get; init; }
}
