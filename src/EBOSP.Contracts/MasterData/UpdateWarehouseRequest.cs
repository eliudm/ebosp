using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.MasterData;

public sealed class UpdateWarehouseRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}
