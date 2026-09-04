using System.ComponentModel.DataAnnotations;

namespace EBOSP.Contracts.Sales;

public sealed class CreateSalesOrderRequest
{
    [Required]
    public required Guid QuotationId { get; init; }

    [Required]
    public required Guid WarehouseId { get; init; }
}
