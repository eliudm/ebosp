using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class TransferStockRequest
{
    [Required]
    public required Guid FromWarehouseId { get; init; }

    [Required]
    public required Guid ToWarehouseId { get; init; }

    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}
