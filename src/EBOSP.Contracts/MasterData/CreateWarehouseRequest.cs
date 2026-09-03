using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.MasterData;

public sealed class CreateWarehouseRequest
{
    [Required]
    public required Guid BranchId { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}
