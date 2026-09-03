using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.MasterData;

public sealed class UpdateProductRequest
{
    public Guid? CategoryId { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; init; }

    [Range(0, 100)]
    public decimal TaxRatePercent { get; init; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; init; }
}
