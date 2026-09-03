using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Procurement;

public sealed class CreatePurchaseRequestLine
{
    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal EstimatedUnitPrice { get; init; }
}

public sealed class CreatePurchaseRequestRequest
{
    [Required]
    public required Guid BranchId { get; init; }

    [Required]
    [MaxLength(1000)]
    public required string Justification { get; init; }

    [Required]
    public required DateTimeOffset RequiredDate { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<CreatePurchaseRequestLine> Lines { get; init; }
}
