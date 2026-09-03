namespace EBOSP.Application.Procurement;

/// <summary>Bound from the "Procurement" configuration section - a configurable value, not a hardcoded constant (dev guide §47 flags hardcoded approval thresholds as a mistake to avoid).</summary>
public sealed class ProcurementOptions
{
    public const string SectionName = "Procurement";

    /// <summary>Purchase requests with an estimated value above this also require procurement.approve.large to approve - mirrors InventoryOptions.LargeAdjustmentThreshold.</summary>
    public decimal HighValueThreshold { get; init; } = 10000m;
}
