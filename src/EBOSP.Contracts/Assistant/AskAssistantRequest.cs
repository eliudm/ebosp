using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Assistant;

public sealed class AskAssistantRequest
{
    [Required]
    [MaxLength(500)]
    public required string Question { get; init; }
}
