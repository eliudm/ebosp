using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Sales;

public sealed class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Range(0, double.MaxValue)]
    public decimal CreditLimit { get; init; }
}
