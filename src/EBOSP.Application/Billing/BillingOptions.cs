namespace EBOSP.Application.Billing;

/// <summary>Bound from the "Billing" configuration section - a configurable value, not a hardcoded constant (dev guide §47 flags hardcoded approval thresholds as a mistake to avoid).</summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>Payments with an Amount above this also require payment.create.large to confirm (spec §15: "Finance role + transaction limit").</summary>
    public decimal LargePaymentThreshold { get; init; } = 10000m;
}
