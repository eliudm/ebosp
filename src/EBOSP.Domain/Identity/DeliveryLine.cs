using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public sealed class DeliveryLine : Entity, ITenantOwned
{
    private DeliveryLine()
    {
    }

    internal static DeliveryLine Create(Guid tenantId, Guid deliveryId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        return new DeliveryLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeliveryId = deliveryId,
            ProductId = productId,
            Quantity = quantity,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid DeliveryId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }
}
