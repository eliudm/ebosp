using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class AdjustStockRequest
{
    [Required]
    public required Guid WarehouseId { get; init; }

    [Required]
    public required Guid ProductId { get; init; }

    /// <summary>
    /// Positive to increase, negative to decrease; cannot be zero. Excludes int.MinValue - the
    /// controller's threshold check calls Math.Abs(QuantityDelta), which throws OverflowException
    /// for that one value since it has no positive two's-complement counterpart.
    /// </summary>
    [Range(-2147483647, int.MaxValue)]
    public required int QuantityDelta { get; init; }

    [Required]
    [MaxLength(500)]
    public required string Reason { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}
