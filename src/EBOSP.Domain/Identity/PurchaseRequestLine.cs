using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public sealed class PurchaseRequestLine : Entity, ITenantOwned
{
    private PurchaseRequestLine()
    {
    }

    internal static PurchaseRequestLine Create(Guid tenantId, Guid purchaseRequestId, Guid productId, int quantity, decimal estimatedUnitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (estimatedUnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedUnitPrice), "Estimated unit price cannot be negative.");
        }

        return new PurchaseRequestLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseRequestId = purchaseRequestId,
            ProductId = productId,
            Quantity = quantity,
            EstimatedUnitPrice = estimatedUnitPrice,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid PurchaseRequestId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }

    public decimal EstimatedUnitPrice { get; private init; }

    public decimal EstimatedLineTotal => Quantity * EstimatedUnitPrice;
}
