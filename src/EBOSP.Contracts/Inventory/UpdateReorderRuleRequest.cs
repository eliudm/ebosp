using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Inventory;

public sealed class UpdateReorderRuleRequest
{
    [Range(0, int.MaxValue)]
    public required int ReorderLevel { get; init; }
}
