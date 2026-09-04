using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Security;

public sealed class ResolveSecurityAlertRequest
{
    [Required]
    [MaxLength(1000)]
    public required string Notes { get; init; }
}
