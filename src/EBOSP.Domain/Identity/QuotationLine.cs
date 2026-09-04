using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public sealed class QuotationLine : Entity, ITenantOwned
{
    private QuotationLine()
    {
    }

    internal static QuotationLine Create(Guid tenantId, Guid quotationId, Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        return new QuotationLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            QuotationId = quotationId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid QuotationId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }

    public decimal UnitPrice { get; private init; }

    public decimal LineTotal => Quantity * UnitPrice;
}
