using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum SupplierStatus
{
    Active,
    Inactive,
}

/// <summary>Vendor master (ERD: SupplierId, Name, TaxId, Contact) - not assigned to a module by either
/// governing doc; built alongside PurchaseOrder since that's the only place it's referenced.</summary>
public sealed class Supplier : Entity, ITenantOwned
{
    private Supplier()
    {
    }

    public static Supplier Create(Guid tenantId, string name, string? taxId, string? contact)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Supplier name is required.", nameof(name));
        }

        return new Supplier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
            Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim(),
            Status = SupplierStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public string Name { get; private set; } = null!;

    public string? TaxId { get; private set; }

    public string? Contact { get; private set; }

    public SupplierStatus Status { get; private set; }

    public void Update(string name, string? taxId, string? contact)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Supplier name is required.", nameof(name));
        }

        Name = name.Trim();
        TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
    }

    public void Activate() => Status = SupplierStatus.Active;

    public void Deactivate() => Status = SupplierStatus.Inactive;
}
