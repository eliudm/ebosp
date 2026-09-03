namespace EBOSP.Application.Inventory;

/// <summary>Bound from the "Inventory" configuration section - a configurable value, not a hardcoded constant (dev guide §47 flags hardcoded approval thresholds as a mistake to avoid).</summary>
public sealed class InventoryOptions
{
    public const string SectionName = "Inventory";

    /// <summary>Adjustments with |delta| above this also require inventory.adjust.large (spec §16: "Large stock adjustment - configurable threshold").</summary>
    public int LargeAdjustmentThreshold { get; init; } = 100;
}
