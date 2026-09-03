using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Procurement;

public sealed class CreatePurchaseOrderLine
{
    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal UnitPrice { get; init; }
}

public sealed class CreatePurchaseOrderRequest
{
    [Required]
    public required Guid PurchaseRequestId { get; init; }

    [Required]
    public required Guid SupplierId { get; init; }

    [Required]
    [MaxLength(50)]
    public required string PoNumber { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<CreatePurchaseOrderLine> Lines { get; init; }
}
