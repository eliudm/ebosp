using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public sealed class GoodsReceiptLine : Entity, ITenantOwned
{
    private GoodsReceiptLine()
    {
    }

    internal static GoodsReceiptLine Create(Guid tenantId, Guid goodsReceiptId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        return new GoodsReceiptLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GoodsReceiptId = goodsReceiptId,
            ProductId = productId,
            Quantity = quantity,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid GoodsReceiptId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }
}
