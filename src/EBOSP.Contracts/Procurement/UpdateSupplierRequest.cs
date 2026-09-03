using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Procurement;

public sealed class UpdateSupplierRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [MaxLength(50)]
    public string? TaxId { get; init; }

    [MaxLength(200)]
    public string? Contact { get; init; }
}
