using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class CreateReorderRuleRequest
{
    [Required]
    public required Guid WarehouseId { get; init; }

    [Required]
    public required Guid ProductId { get; init; }

    [Range(0, int.MaxValue)]
    public required int ReorderLevel { get; init; }
}
