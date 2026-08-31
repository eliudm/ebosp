using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Identity;

public sealed class AssignRoleRequest
{
    [Required]
    public required string RoleCode { get; init; }

    public Guid? BranchId { get; init; }
}
