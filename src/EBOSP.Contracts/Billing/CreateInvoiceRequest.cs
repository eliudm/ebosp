using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Billing;

public sealed class CreateInvoiceRequest
{
    [Required]
    public required Guid SalesOrderId { get; init; }
}
