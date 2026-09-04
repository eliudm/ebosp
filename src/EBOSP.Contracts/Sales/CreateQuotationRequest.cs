using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Sales;

public sealed class CreateQuotationLine
{
    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal UnitPrice { get; init; }
}

public sealed class CreateQuotationRequest
{
    [Required]
    public required Guid CustomerId { get; init; }

    [Required]
    public required Guid BranchId { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<CreateQuotationLine> Lines { get; init; }
}
