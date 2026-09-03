using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.MasterData;

public sealed class CreateBranchRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}
