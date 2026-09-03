using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.MasterData;

public sealed class CreateProductCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}
