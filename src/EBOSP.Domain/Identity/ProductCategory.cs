using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum ProductCategoryStatus
{
    Active,
    Inactive,
}

/// <summary>Flat product grouping (dev guide §12: "create product categories and products") - no hierarchy, neither governing document describes one.</summary>
public sealed class ProductCategory : Entity, ITenantOwned
{
    private ProductCategory()
    {
    }

    public static ProductCategory Create(Guid tenantId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        return new ProductCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Status = ProductCategoryStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public string Name { get; private set; } = null!;

    public ProductCategoryStatus Status { get; private set; }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Activate() => Status = ProductCategoryStatus.Active;

    public void Deactivate() => Status = ProductCategoryStatus.Inactive;
}
