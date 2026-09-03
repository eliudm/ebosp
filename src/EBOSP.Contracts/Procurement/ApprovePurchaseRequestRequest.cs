using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Procurement;

public sealed class ApprovePurchaseRequestRequest
{
    [MaxLength(1000)]
    public string? Notes { get; init; }
}
