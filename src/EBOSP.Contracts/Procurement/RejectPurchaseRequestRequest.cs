using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Procurement;

public sealed class RejectPurchaseRequestRequest
{
    [Required]
    [MaxLength(1000)]
    public required string Notes { get; init; }
}
