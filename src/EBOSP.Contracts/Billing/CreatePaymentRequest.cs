using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Billing;

/// <summary>
/// Deliberately carries no status/"paid" field - a payment always starts Pending. Only the
/// separate, authorized Confirm/Fail actions can ever settle it (dev guide §16: "never trust a
/// client-provided 'paid' flag").
/// </summary>
public sealed class CreatePaymentRequest
{
    [Required]
    public required Guid InvoiceId { get; init; }

    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; init; }

    [Required]
    [MaxLength(100)]
    public required string Method { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}
