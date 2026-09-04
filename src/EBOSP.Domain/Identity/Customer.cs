using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum CustomerStatus
{
    Active,
    Inactive,
}

/// <summary>Sales counterparty (ERD: CustomerId, Name, CreditLimit).</summary>
public sealed class Customer : Entity, ITenantOwned
{
    private Customer()
    {
    }

    public static Customer Create(Guid tenantId, string name, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (creditLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit), "Credit limit cannot be negative.");
        }

        return new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            CreditLimit = creditLimit,
            Status = CustomerStatus.Active,
        };
    }

    public Guid TenantId { get; private init; }

    public string Name { get; private set; } = null!;

    public decimal CreditLimit { get; private set; }

    public CustomerStatus Status { get; private set; }

    public void Update(string name, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (creditLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit), "Credit limit cannot be negative.");
        }

        Name = name.Trim();
        CreditLimit = creditLimit;
    }

    public void Activate() => Status = CustomerStatus.Active;

    public void Deactivate() => Status = CustomerStatus.Inactive;
}
