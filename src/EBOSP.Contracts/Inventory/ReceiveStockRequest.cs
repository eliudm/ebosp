using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class ReceiveStockLine
{
    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }
}

public sealed class ReceiveStockRequest
{
    [Required]
    public required Guid WarehouseId { get; init; }

    public Guid? PurchaseOrderId { get; init; }

    [MaxLength(200)]
    public string? Reference { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<ReceiveStockLine> Lines { get; init; }
}
