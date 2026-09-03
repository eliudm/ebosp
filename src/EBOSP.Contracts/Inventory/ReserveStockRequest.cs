using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class ReserveStockRequest
{
    [Required]
    public required Guid WarehouseId { get; init; }

    [Required]
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }
}
