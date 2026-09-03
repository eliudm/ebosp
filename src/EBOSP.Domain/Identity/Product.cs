using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum ProductStatus
{
    Active,
    Inactive,
}

/// <summary>Item master (dev guide §12, spec §7: ProductId, SKU, Name, TaxCode, ReorderLevel).</summary>
public sealed class Product : Entity, ITenantOwned
{
    private Product()
    {
    }

    public static Product Create(
        Guid tenantId,
        Guid? categoryId,
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        decimal taxRatePercent,
        int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (taxRatePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRatePercent), "Tax rate cannot be negative.");
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel), "Reorder level cannot be negative.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = categoryId,
            Sku = sku.Trim(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UnitPrice = unitPrice,
            TaxRatePercent = taxRatePercent,
            ReorderLevel = reorderLevel,
            Status = ProductStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid? CategoryId { get; private set; }

    /// <summary>Immutable after creation - an external-facing reference key, not something to silently reassign.</summary>
    public string Sku { get; private init; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TaxRatePercent { get; private set; }

    public int ReorderLevel { get; private set; }

    public ProductStatus Status { get; private set; }

    public void Update(Guid? categoryId, string name, string? description, decimal unitPrice, decimal taxRatePercent, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (taxRatePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRatePercent), "Tax rate cannot be negative.");
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel), "Reorder level cannot be negative.");
        }

        CategoryId = categoryId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UnitPrice = unitPrice;
        TaxRatePercent = taxRatePercent;
        ReorderLevel = reorderLevel;
    }

    public void Activate() => Status = ProductStatus.Active;

    public void Deactivate() => Status = ProductStatus.Inactive;
}
