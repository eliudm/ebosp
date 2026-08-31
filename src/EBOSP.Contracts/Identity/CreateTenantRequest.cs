using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class CreateTenantRequest
{
    [Required]
    [MinLength(2)]
    public required string TenantName { get; init; }

    [Required]
    [EmailAddress]
    public required string AdminEmail { get; init; }

    [Required]
    [MinLength(10)]
    public required string AdminPassword { get; init; }
}
