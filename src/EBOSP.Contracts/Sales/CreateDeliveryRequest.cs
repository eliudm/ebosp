using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Sales;

public sealed class CreateDeliveryRequest
{
    [Required]
    public required Guid SalesOrderId { get; init; }
}
